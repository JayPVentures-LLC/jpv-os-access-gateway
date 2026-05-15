# JPV-OS Access Gateway Design System

## Brand Identity

### Entity Names (Exact Spelling Required)
- **JPV-OS**: Core infrastructure system
- **init**: Application interface
- **JayPVentures LLC**: Enterprise infrastructure authority
- **jaypventures**: Creator ecosystem
- **jaypVLabs**: Research and validation layer
- **JPV Institute**: Standards, doctrine, and institutional research

### Prohibited Public-Facing Language
The following terms must **NOT** appear in any public-facing content (pages, headers, descriptions, etc.):
- ❌ "division"
- ❌ "master"
- ❌ "control"

These terms may only appear in technical/internal code contexts if absolutely unavoidable.

### Preferred Language
Use these terms for operational and governance descriptions:
- ✅ Orchestration
- ✅ Routing
- ✅ Governance
- ✅ Operational integrity
- ✅ Infrastructure authority
- ✅ Alignment
- ✅ Coordination
- ✅ Structured execution
- ✅ Access gateway
- ✅ Validation layer

## Visual Identity

### Color Palette

| Color | Hex | Usage |
|-------|-----|-------|
| Deep Black | #0A0A0A | Primary background |
| Navy | #0F1419 | Secondary background, panels |
| Electric Blue | #00D4FF | Primary accent, buttons, highlights |
| Royal Purple | #7B30FF | Secondary accent, gradients |
| Magenta | #FF2D8A | Tertiary accent, alerts |
| Silver/Chrome | #E0E0E0 | Typography, icons |
| Subtle Gray | #3A3A3A | Borders, dividers |

### Typography

- **Headlines**: Chrome/Silver on dark background, generous letter-spacing
- **Body**: Chrome/Silver with 16px-18px size, 1.6 line height
- **Accents**: Electric blue for emphasis, Royal purple for secondary callouts

### Layout & Surfaces

- **Glass Panels**: 10-15% opacity white background with subtle blur effect
- **Gradients**: Electric blue to Royal purple, used for hero sections and CTAs
- **Glow Effects**: Subtle #00D4FF glow on interactive elements (not gaming-style RGB)
- **Spacing**: 16px, 24px, 32px, 48px grid

## Component Guidelines

### Button States
- **Primary**: Electric blue background with white text
- **Secondary**: Navy border with Electric blue text
- **Disabled**: Gray background with reduced opacity
- **Hover**: Subtle glow, slight scale increase (1.02x)
- **Active**: Royal purple accent border

### Cards
- Background: Navy with 1px Electric blue border
- Glass effect with subtle backdrop blur
- Rounded corners: 8px
- Padding: 24px
- Shadow: None (use border and glow instead)

### Navigation
- Dark background (Deep black)
- Icons: Silver/Chrome
- Active state: Electric blue underline
- Mobile: Hamburger menu with slide-out panel (Electric blue accent)

### Hero Sections
- Full-width background with subtle gradient (Electric blue to Royal purple)
- White/Chrome text
- CTA buttons with Electric blue background

## Content Structure

### Required Pages
1. **Home** - Hero section, ecosystem overview, getting started CTAs
2. **Pricing** - Tier cards for member, creator, partner, enterprise, custom
3. **Partners** - Partner listings and partner portal access info
4. **Access/Login** - Route selector for different access paths
5. **About/Founder** - Founder story, company mission, governance
6. **Admin Placeholder** - Governance and administrative interface
7. **Ecosystem Overview** - Visual representation of jaypventures, jaypVLabs, JPV Institute

### CTA Text Standards
- "Get init" - For membership signup
- "Route Access" - For access path selection
- "Request Access" - For higher-tier packages
- "Contact" - For custom implementations
- "Learn More" - For information sections

## Responsive Design

- **Mobile-first** approach
- **Breakpoints**: 320px, 640px, 1024px, 1440px
- **Grid**: 12-column on desktop, 4-column on tablet, single-column on mobile
- **Navigation**: Full-width on mobile with side drawer, horizontal on desktop
- **Font Sizing**: Responsive with clamp() function

## Accessibility

- **Color Contrast**: WCAG AA compliance (4.5:1 for text)
- **Focus States**: Visible Electric blue focus outline (2px)
- **Labels**: All form inputs have associated labels
- **Alt Text**: All images include descriptive alt text
- **Semantic HTML**: Proper heading hierarchy (h1 → h6)
- **Skip Links**: Navigation skip link available

## Animation & Motion

- **Duration**: 200-300ms for interactive states
- **Easing**: ease-out for appearance, ease-in-out for motion
- **Preference**: Respect `prefers-reduced-motion` media query
- **Effects**: Subtle transitions, no excessive motion

## Telerik Integration Ready

The design system is structured to accommodate Telerik UI components:

- Component styling follows Telerik themes
- Color variables can be swapped for Telerik color tokens
- Glass panel styling matches Telerik card components
- All Blazor components use standard Telerik naming conventions
- Custom adapters allow seamless component replacement

See `UI-COMPONENT-STRATEGY.md` for detailed Telerik readiness information.
