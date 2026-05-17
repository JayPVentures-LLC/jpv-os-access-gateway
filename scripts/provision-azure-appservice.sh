#!/bin/bash
set -euo pipefail

# JPV-OS Access Gateway - Azure App Service Provisioning Script
# 
# This script automates Azure App Service provisioning with comprehensive validation:
# 1. Validates Azure tenant access and subscription authority
# 2. Validates regional quota availability
# 3. Provisions resource group, App Service plan, and Web App
# 4. Generates publish profile and configures GitHub secret
# 5. Triggers deployment workflow

# Configuration
RESOURCE_GROUP_NAME="${1:-rg-jpv-os-prod}"
APP_SERVICE_PLAN_NAME="${2:-asp-jpv-os-prod}"
WEB_APP_NAME="${3:-jpv-os-access-gateway}"
SKU_NAME="${4:-B1}"
REGIONS="${5:-eastus,centralus,eastus2,westus2}"

# Color codes
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;36m'
NC='\033[0m' # No Color

# Output helpers
write_success() {
    echo -e "${GREEN}✓ $1${NC}"
}

write_warning() {
    echo -e "${YELLOW}⚠ $1${NC}"
}

write_error() {
    echo -e "${RED}✗ $1${NC}"
}

write_info() {
    echo -e "${BLUE}ℹ $1${NC}"
}

test_azure_prerequisites() {
    write_info "Checking Azure CLI prerequisites..."
    
    if ! command -v az &> /dev/null; then
        write_error "Azure CLI is not installed"
        echo "Please install it from: https://aka.ms/azure-cli"
        exit 1
    fi
    
    local az_version=$(az version --query '."azure-cli"' -o tsv 2>/dev/null || echo "unknown")
    write_success "Azure CLI version: $az_version"
    
    if command -v gh &> /dev/null; then
        write_success "GitHub CLI available"
    else
        write_warning "GitHub CLI (gh) not installed. You'll need to manually set AZURE_WEBAPP_PUBLISH_PROFILE secret."
    fi
}

test_azure_login() {
    write_info "Validating Azure login status..."
    
    if ! az account show &>/dev/null; then
        write_error "Not logged into Azure. Run: az login"
        exit 1
    fi
    
    local account=$(az account show --query '{user:user.name, tenantId:tenantId}' -o json)
    local user=$(echo "$account" | jq -r '.user')
    local tenant=$(echo "$account" | jq -r '.tenantId')
    
    write_success "Logged in as: $user"
    write_success "Current tenant: $tenant"
}

get_available_subscriptions() {
    write_info "Fetching available subscriptions..."
    
    local subs=$(az account list --query "[].{name:name, id:id, tenantId:tenantId, state:state}" -o json)
    
    if [ -z "$subs" ] || [ "$subs" = "[]" ]; then
        write_error "No subscriptions found. The current account has no subscriptions assigned."
        write_error ""
        write_error "Remediation:"
        write_error "1. Contact Azure administrator to assign a subscription to your account"
        write_error "2. For JayPVentures LLC tenant: Request subscription assignment in Azure portal"
        write_error "3. Re-run this script after subscription is assigned"
        exit 1
    fi
    
    write_info "Found subscriptions:"
    echo "$subs" | jq -r '.[] | "  - \(.name) [\(.id)]"'
    
    echo "$subs"
}

select_subscription() {
    local subscriptions=$1
    local count=$(echo "$subscriptions" | jq 'length')
    
    if [ "$count" -eq 1 ]; then
        local selected=$(echo "$subscriptions" | jq '.[0]')
    else
        # Try to find JayPVentures subscription
        local selected=$(echo "$subscriptions" | jq '.[] | select(.name | test("JayPVentures|jpv"; "i"))' | head -1)
        if [ -z "$selected" ]; then
            selected=$(echo "$subscriptions" | jq '.[0]')
        fi
    fi
    
    local sub_name=$(echo "$selected" | jq -r '.name')
    local sub_id=$(echo "$selected" | jq -r '.id')
    
    az account set --subscription "$sub_id"
    write_success "Selected subscription: $sub_name [$sub_id]"
    
    echo "$selected"
}

test_subscription_authority() {
    local subscription_id=$1
    write_info "Validating subscription authority..."
    
    local roles=$(az role assignment list --subscription "$subscription_id" --query "[].roleDefinitionName" -o json | jq -r '.[]' | sort -u)
    
    if echo "$roles" | grep -qE "Owner|Contributor|Website Contributor"; then
        write_success "Has required role for resource creation"
        return 0
    else
        write_error "Current account does not have required roles for resource creation"
        write_error "Requires one of: Owner, Contributor, Website Contributor"
        write_error "Current roles: $roles"
        exit 1
    fi
}

test_provider_registration() {
    local subscription_id=$1
    write_info "Checking Microsoft.Web provider registration..."
    
    local state=$(az provider show --namespace Microsoft.Web --subscription "$subscription_id" --query "registrationState" -o tsv)
    
    if [ "$state" = "Registered" ]; then
        write_success "Microsoft.Web provider is registered"
        return 0
    fi
    
    write_warning "Microsoft.Web provider not registered. Attempting registration..."
    
    if az provider register --namespace Microsoft.Web --subscription "$subscription_id" &>/dev/null; then
        write_success "Successfully registered Microsoft.Web provider"
        write_info "Waiting for provider registration to propagate..."
        sleep 5
        return 0
    else
        write_error "Failed to register Microsoft.Web provider"
        write_error "Remediation: Register provider manually or contact Azure support"
        exit 1
    fi
}

find_viable_region() {
    local sku=$1
    local regions_str=$2
    
    write_info "Validating regional quota for SKU: $sku..."
    write_info "Checking regions: $regions_str"
    
    local IFS=','
    for region in $regions_str; do
        region=$(echo "$region" | xargs) # trim whitespace
        write_info "Checking quota in $region..."
        
        if az appservice plan create \
            --name "test-sku-check-$(date +%s)" \
            --resource-group "test-rg-$(date +%s)" \
            --location "$region" \
            --sku "$sku" \
            --is-linux \
            --dry-run &>/dev/null; then
            
            write_success "Region $region has available quota"
            echo "$region"
            return 0
        else
            write_warning "Region $region not viable"
        fi
    done
    
    write_error ""
    write_error "No viable regions found with available quota for SKU: $sku"
    write_error ""
    write_error "Remediation steps:"
    write_error "1. Log into Azure Portal: https://portal.azure.com"
    write_error "2. Navigate to Subscriptions > Usage + quotas"
    write_error "3. For each region (East US, Central US, East US 2, West US 2):"
    write_error "   - Check 'Compute' category"
    write_error "   - Look for 'Total VM quota'"
    write_error "   - If quota is 0, click 'Request quota increase'"
    write_error "   - Select 'New support request' if needed"
    write_error "4. Once quotas are increased, re-run this script"
    exit 1
}

new_azure_resources() {
    local rg=$1
    local plan=$2
    local webapp=$3
    local sku=$4
    local region=$5
    
    write_info "Creating Azure resources in region: $region"
    
    # Create resource group
    write_info "Creating resource group: $rg"
    if ! az group create --name "$rg" --location "$region" --output none; then
        write_error "Failed to create resource group"
        exit 1
    fi
    write_success "Resource group created"
    
    # Create App Service Plan
    write_info "Creating App Service plan: $plan"
    if ! az appservice plan create \
        --name "$plan" \
        --resource-group "$rg" \
        --location "$region" \
        --sku "$sku" \
        --is-linux \
        --output none; then
        
        write_error "Failed to create App Service plan"
        write_error "This is a blocker. Do not continue."
        exit 1
    fi
    write_success "App Service plan created"
    
    # Create Web App
    write_info "Creating .NET 8 Web App: $webapp"
    if ! az webapp create \
        --name "$webapp" \
        --resource-group "$rg" \
        --plan "$plan" \
        --runtime "DOTNET|8.0" \
        --output none; then
        
        write_error "Failed to create Web App"
        exit 1
    fi
    write_success "Web App created"
    
    # Configure HTTPS-only
    write_info "Configuring HTTPS enforcement"
    az webapp update \
        --name "$webapp" \
        --resource-group "$rg" \
        --https-only true \
        --output none
    write_success "HTTPS-only enabled"
    
    # Set app settings
    write_info "Configuring app settings"
    az webapp config appsettings set \
        --name "$webapp" \
        --resource-group "$rg" \
        --settings WEBSITES_ENABLE_APP_SERVICE_STORAGE=true \
        --output none
    write_success "App settings configured"
}

get_publish_profile() {
    local webapp=$1
    local rg=$2
    
    write_info "Generating publish profile..."
    
    local profile_path="azure-publish-profile-${webapp}.xml"
    
    if ! az webapp deployment list-publishing-profiles \
        --name "$webapp" \
        --resource-group "$rg" > "$profile_path" 2>&1; then
        
        write_error "Failed to generate publish profile"
        exit 1
    fi
    
    if [ -f "$profile_path" ]; then
        write_success "Publish profile generated: $profile_path"
        echo "$profile_path"
    else
        write_error "Failed to generate publish profile"
        exit 1
    fi
}

set_github_secret() {
    local profile_path=$1
    
    write_info "Configuring GitHub deployment secret..."
    
    if ! command -v gh &> /dev/null; then
        write_warning "GitHub CLI (gh) not available. Manual configuration needed:"
        write_warning "1. Read publish profile: cat '$profile_path'"
        write_warning "2. Copy the XML content"
        write_warning "3. In GitHub: Settings > Secrets and variables > Actions"
        write_warning "4. Create new secret: AZURE_WEBAPP_PUBLISH_PROFILE"
        write_warning "5. Paste the XML content"
        return 1
    fi
    
    write_info "Setting GitHub secret: AZURE_WEBAPP_PUBLISH_PROFILE"
    
    if cat "$profile_path" | gh secret set AZURE_WEBAPP_PUBLISH_PROFILE --body-file -; then
        write_success "GitHub secret configured successfully"
        return 0
    else
        write_error "Failed to set GitHub secret"
        write_warning "Manual configuration needed. See instructions above."
        return 1
    fi
}

trigger_deployment() {
    local webapp=$1
    
    write_info "Triggering GitHub Actions deployment..."
    
    if ! command -v gh &> /dev/null; then
        write_warning "GitHub CLI (gh) not available. Manual trigger needed:"
        write_warning "1. Go to: https://github.com/JayPVentures-LLC/jpv-os-access-gateway"
        write_warning "2. Actions > deploy-appservice"
        write_warning "3. Click 'Run workflow'"
        return 1
    fi
    
    if gh workflow run deploy-appservice.yml; then
        write_success "Deployment workflow triggered"
        write_info "Waiting for deployment to complete (this may take several minutes)..."
        sleep 30
        return 0
    else
        write_error "Failed to trigger deployment"
        write_warning "Manual trigger needed. See instructions above."
        return 1
    fi
}

test_health_endpoint() {
    local webapp=$1
    
    write_info "Validating application health endpoint..."
    
    local url="https://${webapp}.azurewebsites.net/api/health"
    local max_attempts=30
    local attempt=0
    
    while [ $attempt -lt $max_attempts ]; do
        attempt=$((attempt + 1))
        
        if curl -s -k -f "$url" > /dev/null 2>&1; then
            write_success "Health endpoint is responding: $url"
            local response=$(curl -s -k "$url")
            write_success "Response: $response"
            return 0
        fi
        
        if [ $attempt -eq 1 ]; then
            write_info "Waiting for application to become available ($attempt/$max_attempts)..."
        elif [ $((attempt % 5)) -eq 0 ]; then
            write_info "Still waiting... ($attempt/$max_attempts)"
        fi
        
        sleep 10
    done
    
    write_warning "Health endpoint not responding after $((max_attempts * 10 / 60)) minutes"
    write_warning "The application may still be deploying. Check Azure Portal for status."
    return 1
}

show_summary() {
    local rg=$1
    local webapp=$2
    local profile_path=$3
    local health_ok=$4
    
    echo ""
    write_success "========================================="
    write_success "PROVISIONING COMPLETE"
    write_success "========================================="
    echo ""
    write_info "Resource Details:"
    echo "  Resource Group:    $rg"
    echo "  Web App Name:      $webapp"
    echo "  URL:               https://${webapp}.azurewebsites.net"
    echo "  Health Endpoint:   https://${webapp}.azurewebsites.net/api/health"
    echo ""
    write_info "Next Steps:"
    echo "  1. Verify: https://${webapp}.azurewebsites.net"
    echo "  2. Configure custom domain if needed"
    echo "  3. Update Cloudflare DNS to point to Azure App Service"
    echo ""
    
    if [ "$health_ok" = "true" ]; then
        write_success "Application is healthy and ready for production"
    else
        write_warning "Application health status unknown. Check Azure Portal."
    fi
    
    echo "  Publish Profile:   $profile_path"
    echo ""
}

# Main execution
echo ""
echo "========================================="
echo "JPV-OS Access Gateway - Azure Provisioning"
echo "========================================="
echo ""

# Step 1: Check prerequisites
test_azure_prerequisites

# Step 2: Validate Azure login
test_azure_login

# Step 3: Get available subscriptions
subscriptions=$(get_available_subscriptions)

# Step 4: Select subscription
subscription=$(select_subscription "$subscriptions")

# Step 5: Validate subscription authority
subscription_id=$(echo "$subscription" | jq -r '.id')
test_subscription_authority "$subscription_id"

# Step 6: Test provider registration
test_provider_registration "$subscription_id"

# Step 7: Find viable region
viable_region=$(find_viable_region "$SKU_NAME" "$REGIONS")

# Step 8: Provision resources
new_azure_resources "$RESOURCE_GROUP_NAME" "$APP_SERVICE_PLAN_NAME" "$WEB_APP_NAME" "$SKU_NAME" "$viable_region"

# Step 9: Get publish profile
profile_path=$(get_publish_profile "$WEB_APP_NAME" "$RESOURCE_GROUP_NAME")

# Step 10: Configure GitHub secret
secret_ok=false
if set_github_secret "$profile_path"; then
    secret_ok=true
fi

# Step 11: Trigger deployment
deployment_ok=false
if [ "$secret_ok" = "true" ]; then
    if trigger_deployment "$WEB_APP_NAME"; then
        deployment_ok=true
    fi
fi

# Step 12: Test health endpoint
health_ok=false
if [ "$deployment_ok" = "true" ]; then
    if test_health_endpoint "$WEB_APP_NAME"; then
        health_ok=true
    fi
fi

# Show summary
show_summary "$RESOURCE_GROUP_NAME" "$WEB_APP_NAME" "$profile_path" "$health_ok"

echo ""
