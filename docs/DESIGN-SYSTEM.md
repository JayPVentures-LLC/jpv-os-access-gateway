# JPV-OS Design System

The JPV-OS Design System defines a unified, premium visual language for the Blazor application. All styling adheres to a dark, cinematic aesthetic with carefully controlled use of electric accent colors.

## Design Principles

- **Premium & Cinematic**: Dark backgrounds with selective color accents create depth and sophistication
- **Readable & Accessible**: High contrast typography on dark surfaces ensures legibility
- **Consistent**: Centralized CSS variables enforce consistency across all components
- **Glass Morphism**: Semi-transparent panels with backdrop blur create dimensional depth
- **Glow & Accent**: Electric colors (#00D4FF, #7B30FF, #FF2D8A) provide visual hierarchy and interaction feedback

## Color Palette

### Primary Colors

| Variable | Value | Usage |
|----------|-------|-------|
| `--jpv-black` | #05070B | Core background |
| `--jpv-navy` | #0A1020 | Secondary background |
| `--jpv-panel` | rgba(8,12,24,.88) | Glass panels - opaque |
| `--jpv-panel-soft` | rgba(255,255,255,.035) | Subtle background layers |

### Accent Colors (Electric)

| Variable | Value | Usage |
|----------|-------|-------|
| `--jpv-cyan` | #00D4FF | Primary accent, highlights, focus states |
| `--jpv-purple` | #7B30FF | Secondary accent, tertiary elements |
| `--jpv-magenta` | #FF2D8A | Tertiary accent, call-to-action, warnings |

### Typography & Dividers

| Variable | Value | Usage |
|----------|-------|-------|
| `--jpv-text` | #F5F7FA | Primary text, high contrast |
| `--jpv-muted` | #B8C2D9 | Secondary text, supporting content |
| `--jpv-muted-2` | #8FA3C7 | Tertiary text, metadata |
| `--jpv-line` | rgba(255,255,255,.075) | Borders, dividers |
| `--jpv-line-soft` | rgba(255,255,255,.055) | Subtle separators |

## Spacing Scale

Spacing follows an 8px base unit:

| Variable | Value | Usage |
|----------|-------|-------|
| `--jpv-space-xs` | 4px | Minimal spacing (line-height adjustments) |
| `--jpv-space-sm` | 8px | Small gaps, padding in compact components |
| `--jpv-space-md` | 16px | Standard padding and margins |
| `--jpv-space-lg` | 24px | Large spacing, component gutters |
| `--jpv-space-xl` | 32px | Extra large sections, vertical rhythm |
| `--jpv-space-2xl` | 48px | Hero sections, major layout divisions |
| `--jpv-space-3xl` | 64px | Full-page sections, padding |

## Border Radius Scale

Rounded corners reinforce the premium aesthetic:

| Variable | Value | Usage |
|----------|-------|-------|
| `--jpv-radius-sm` | 4px | Minimal rounding (buttons, small elements) |
| `--jpv-radius-md` | 8px | Standard rounding (cards, inputs) |
| `--jpv-radius-lg` | 16px | Large elements (large cards, panels) |
| `--jpv-radius-xl` | 24px | Extra large (hero panels, featured cards) |
| `--jpv-radius-full` | 999px | Fully rounded (badges, pills) |

## Typography

### Font Families

- **Primary**: Inter (400, 600, 700, 800, 900)
- **Display**: Space Grotesk (500, 600, 700)
- **Fallback**: Segoe UI, Arial, sans-serif

### Font Sizes

Semantic sizing using CSS `clamp()` for responsive scaling:

- **Heading 1** (H1): `var(--jpv-text-3xl)` = `clamp(48px, 8vw, 140px)` — Page titles, hero text
- **Heading 2** (H2): `var(--jpv-text-2xl)` = `clamp(36px, 6vw, 96px)` — Section titles
- **Heading 3** (H3): `var(--jpv-text-xl)` = `clamp(28px, 4vw, 64px)` — Subsections
- **Body Large**: `var(--jpv-text-lg)` = `clamp(18px, 1.5vw, 26px)` — Hero subtitles, prominent content
- **Body Regular**: `var(--jpv-text-base)` = 16px — Standard body text
- **Body Small**: `var(--jpv-text-sm)` = 14px — Supporting text, labels
- **Eyebrow/Kicker**: `var(--jpv-text-xs)` = 13px — Category labels, section prefixes (uppercase, letter-spacing)

### Line Heights

- **Tight**: 0.9 — Display headings
- **Normal**: 1.2 — Headings
- **Relaxed**: 1.6 — Body text
- **Generous**: 1.8 — Long-form content

## Glass Panels (Glassmorphism)

### Base Glass Panel

```css
background: rgba(8, 12, 24, 0.88);
border: 1px solid rgba(255, 255, 255, 0.08);
backdrop-filter: blur(20px);
border-radius: var(--jpv-radius-xl);
```

### Soft Glass Panel

```css
background: rgba(255, 255, 255, 0.03);
border: 1px solid rgba(255, 255, 255, 0.08);
backdrop-filter: blur(12px);
```

## Glow & Effects

### Accent Glow (Cyan)

```css
box-shadow: 0 0 40px rgba(0, 212, 255, 0.12);
border-color: rgba(0, 212, 255, 0.4);
```

### Interactive States

- **Hover**: Slight elevation (translateY -6px), enhanced glow
- **Focus**: Primary accent glow with visible border
- **Active**: Maintained glow with opacity increase

## Background Gradients

### Main Background

```css
background:
    radial-gradient(circle at 12% 8%, rgba(0, 212, 255, 0.13), transparent 28%),
    radial-gradient(circle at 88% 4%, rgba(255, 45, 138, 0.11), transparent 26%),
    radial-gradient(circle at 50% 90%, rgba(123, 48, 255, 0.10), transparent 34%),
    linear-gradient(135deg, #03050a 0%, #07111f 48%, #100612 100%);
```

### Grid Overlay

```css
background:
    linear-gradient(rgba(255, 255, 255, 0.022) 1px, transparent 1px),
    linear-gradient(90deg, rgba(255, 255, 255, 0.017) 1px, transparent 1px);
background-size: 72px 72px;
```

## Component Patterns

### Cards

- **Standard Card**: Glass panel with hover elevation and glow
- **Image Card**: Glass panel with contained image, hover effect
- **Content Card**: Glass panel with padding, text hierarchy

### Buttons

- **Primary Button**: Cyan accent, glass background, hover glow
- **Secondary Button**: Purple accent, outlined style
- **Tertiary Button**: Magenta accent, text-only or ghost style

### Forms

- **Input Fields**: Dark glass background, subtle border, cyan focus glow
- **Labels**: Muted typography, uppercase eyebrow style
- **Helper Text**: Muted-2 color, smaller size

## Responsive Design

### Breakpoints

- **Mobile**: < 640px
- **Tablet**: 640px - 1024px
- **Desktop**: > 1024px
- **Wide**: > 1440px

### Layout Constraints

- **Max Width**: `min(1880px, calc(100vw - 56px))`
- **Horizontal Padding**: `clamp(18px, 3vw, 46px)`
- **Vertical Padding**: Responsive using spacing scale

## Animation & Transition

### Standard Transitions

- **UI Elements**: `all 0.4s ease`
- **Hover Effects**: `0.3s cubic-bezier(0.4, 0, 0.2, 1)`
- **Entrance**: `0.6s ease-out`

### Reduce Motion

All transitions and animations respect `prefers-reduced-motion` media query.

## Do's & Don'ts

### Do

✓ Use the centralized CSS variables  
✓ Maintain premium, dark aesthetic  
✓ Layer glass panels for depth  
✓ Apply glow effects sparingly for hierarchy  
✓ Use electric colors for interaction states  
✓ Ensure high contrast for readability  
✓ Test all colors against WCAG AA standards  

### Don't

✗ Use Bootstrap default components  
✗ Apply rainbow or excessive gradients  
✗ Use gaming RGB overload styling  
✗ Create flat, plain card designs  
✗ Reduce opacity below 0.8 for primary text  
✗ Add custom variables outside the system  
✗ Apply colors without hierarchy intention  

## Implementation Notes

- All styling is centralized in `wwwroot/css/jpv-os.tokens.css`
- No component-scoped CSS is used; all styles follow the shared CSS governance model
- The CSS grid overlay creates visual depth without performance impact
- Backdrop filters are essential to the glass aesthetic; ensure browser compatibility
- Font rendering is optimized with system-ui fallbacks and font-smoothing

## Maintenance

When updating the design system:

1. Update the corresponding CSS variable in `jpv-os.tokens.css`
2. Test changes across all major components
3. Verify responsive behavior at all breakpoints
4. Document the change in this file
5. Review for accessibility and readability
