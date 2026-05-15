# Partner Asset Register

This document tracks all partner logo assets used in the JPV-OS Access Gateway. Each asset is categorized by its approval status and documented with trademark/brand guideline notes.

## Asset Types

- **Official**: Exact official brand mark obtained from partner brand resources
- **Approved**: Stylized representation approved for internal use within JPV-OS visual system
- **Fallback**: Internal approximation pending official asset replacement

## Asset Directory

All partner assets are stored in: `src/JPVOS/wwwroot/assets/partners/`

## Partner Asset Registry

| Partner Name | Filename | Asset Type | Source/Owner | Trademark | Notes | Approval |
|--------------|----------|------------|--------------|-----------|-------|----------|
| Apple iCloud | apple-icloud.svg | Fallback | Apple | TM | Stylized cloud representation; official Apple brand guidelines restrict logo usage | Approved Internal |
| Azure | azure.svg | Fallback | Microsoft | TM | Stylized representation using Microsoft Azure brand colors; awaiting official asset | Approved Internal |
| Cloudflare | cloudflare.svg | Fallback | Cloudflare | TM | Stylized shield representation; official mark requires brand approval | Approved Internal |
| Cloudflare Workers | cloudflare-workers.svg | Fallback | Cloudflare | TM | Stylized worker icon; internal representation | Approved Internal |
| Coursera | coursera.svg | Fallback | Coursera | TM | Stylized C lettermark using Coursera blue; brand guidelines pending review | Approved Internal |
| Discord | discord.svg | Fallback | Discord | TM | Stylized clyde/chat icon; Discord brand assets available but require approval | Approved Internal |
| Entra ID | entra-id.svg | Fallback | Microsoft | TM | Stylized identity icon using Microsoft colors; awaiting official Entra ID mark | Approved Internal |
| GitHub | github.svg | Fallback | GitHub | TM | Stylized octocat silhouette; GitHub provides brand kit but requires usage review | Approved Internal |
| GitHub Copilot | github-copilot.svg | Fallback | GitHub | TM | Stylized AI assistant icon; internal representation | Approved Internal |
| Google Workspace | google-workspace.svg | Fallback | Google | TM | Stylized multi-color dots; Google brand guidelines restrict official logo usage | Approved Internal |
| HashiCorp | hashicorp.svg | Fallback | HashiCorp | TM | Stylized H lettermark; HashiCorp brand guidelines pending | Approved Internal |
| HP | hp.svg | Fallback | HP | TM | Stylized HP lettermark using brand blue; HP brand guidelines restrict usage | Approved Internal |
| IBM | ibm.svg | Fallback | IBM | TM | Stylized IBM wordmark with horizontal stripes; IBM brand guidelines pending | Approved Internal |
| iCloud | icloud.svg | Fallback | Apple | TM | Alternate cloud icon; Apple brand restrictions apply | Approved Internal |
| Intel | intel.svg | Fallback | Intel | TM | Stylized intel wordmark using brand blue; Intel brand kit pending | Approved Internal |
| JPV Institute | jpv-institute.svg | Approved | JPV Institute | Internal | JPV-owned brand; approved for all JPV-OS surfaces | Approved |
| Linktree | linktree.svg | Fallback | Linktree | TM | Stylized tree/link icon; Linktree brand assets pending | Approved Internal |
| Microsoft | microsoft.svg | Fallback | Microsoft | TM | Stylized four-square grid; Microsoft brand guidelines pending | Approved Internal |
| Microsoft Security | microsoft-security.svg | Fallback | Microsoft | TM | Stylized shield icon using Microsoft colors; internal representation | Approved Internal |
| Node.js | nodejs.svg | Fallback | OpenJS Foundation | TM | Stylized hexagon icon using Node.js green; official logo pending | Approved Internal |
| OpenAI | openai.svg | Fallback | OpenAI | TM | Stylized hexagon/neural pattern; OpenAI brand guidelines restrict usage | Approved Internal |
| PowerShell | powershell.svg | Fallback | Microsoft | TM | Stylized terminal icon; Microsoft brand pending | Approved Internal |
| Spotify | spotify.svg | Fallback | Spotify | TM | Stylized sound wave icon using Spotify green; Spotify brand kit pending | Approved Internal |
| Stripe | stripe.svg | Fallback | Stripe | TM | Stylized S lettermark using Stripe purple; Stripe brand guidelines pending | Approved Internal |
| University of Phoenix | university-phoenix.svg | Fallback | University of Phoenix | TM | Stylized phoenix flame icon (filename abbreviated to university-phoenix for URL consistency); official mark requires educational partner approval | Approved Internal |
| Unreal Editor | unreal-editor.svg | Fallback | Epic Games | TM | Stylized U lettermark; Epic Games brand guidelines pending | Approved Internal |
| Verizon | verizon.svg | Fallback | Verizon | TM | Stylized checkmark using Verizon red; Verizon brand kit pending | Approved Internal |
| Visual Studio Code | visual-studio-code.svg | Fallback | Microsoft | TM | Stylized code brackets icon; alternate to vscode.svg | Approved Internal |
| VS Code | vscode.svg | Fallback | Microsoft | TM | Stylized VS Code icon using brand blue; Microsoft brand pending | Approved Internal |
| Windows | windows.svg | Fallback | Microsoft | TM | Stylized four-pane window icon; Microsoft brand guidelines pending | Approved Internal |
| Wix | wix.svg | Fallback | Wix.com | TM | Stylized W lettermark; Wix brand assets pending | Approved Internal |
| Xbox | xbox.svg | Fallback | Microsoft | TM | Stylized X sphere using Xbox green; Microsoft/Xbox brand guidelines pending | Approved Internal |

## Fallback Rationale

All current partner assets are styled internal representations designed to:

1. **Maintain visual consistency** with JPV-OS design system (dark theme, rounded containers)
2. **Respect brand guidelines** by not directly copying official marks without approval
3. **Provide functional placeholders** while official brand approvals are obtained
4. **Use recognizable colors/shapes** associated with each partner brand

## Official Asset Replacement Process

To replace a fallback with an official asset:

1. Obtain official brand asset from partner brand resources
2. Verify usage rights per partner brand guidelines
3. Convert to SVG matching JPV-OS container format (96x96 viewBox, rx="22" rounded rect)
4. Replace file in `src/JPVOS/wwwroot/assets/partners/`
5. Update this register with Asset Type: Official and approval date
6. Test in local development before commit

## Internal Contact

For brand/asset questions: JayPVentures LLC Operations
