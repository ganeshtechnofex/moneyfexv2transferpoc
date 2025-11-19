# Testing Guide - MoneyFex Modular Application

## Cross-Browser Testing

### Supported Browsers
- ✅ Chrome/Edge (Latest 2 versions)
- ✅ Firefox (Latest 2 versions)
- ✅ Safari (Latest 2 versions)
- ✅ Opera (Latest version)
- ✅ Mobile browsers (iOS Safari, Chrome Mobile)

### Browser-Specific Features Tested

#### Chrome/Edge
- CSS Grid and Flexbox
- CSS Custom Properties
- ES6+ JavaScript features
- Fetch API

#### Firefox
- CSS Grid and Flexbox
- CSS Custom Properties
- Print styles

#### Safari
- WebKit-specific prefixes
- Touch event handling
- iOS viewport fixes

#### Internet Explorer 11 (Fallback)
- Flexbox with -ms- prefix
- Basic CSS without modern features

## Device Testing

### Desktop
- **1920x1080** (Full HD) - Primary
- **1366x768** (HD) - Common laptop
- **2560x1440** (2K) - High-res displays

### Tablet
- **768x1024** (iPad Portrait)
- **1024x768** (iPad Landscape)
- **800x1280** (Android Tablet)

### Mobile
- **375x667** (iPhone 8)
- **414x896** (iPhone 11 Pro Max)
- **360x640** (Android Standard)

## Responsive Breakpoints

### Mobile First Approach
```css
/* Base: Mobile (< 576px) */
/* Small: 576px+ */
/* Medium: 768px+ */
/* Large: 992px+ */
/* Extra Large: 1200px+ */
```

### Test Scenarios

1. **Navigation Menu**
   - Desktop: Full horizontal menu
   - Tablet: Collapsible menu
   - Mobile: Hamburger menu

2. **Transaction Tables**
   - Desktop: All columns visible
   - Tablet: Some columns hidden
   - Mobile: Essential columns only

3. **Forms**
   - Desktop: Multi-column layout
   - Mobile: Single column, stacked

4. **Currency Calculator**
   - Desktop: Side-by-side layout
   - Mobile: Stacked layout

## Feature Testing Checklist

### Landing Page
- [ ] Currency selection dropdown works
- [ ] Amount calculation updates in real-time
- [ ] Exchange rate displays correctly
- [ ] Transfer method selection works
- [ ] Form validation works
- [ ] Responsive on all devices

### Transaction Forms
- [ ] Bank Deposit form loads banks
- [ ] Mobile Transfer form loads wallet operators
- [ ] Cash Pickup form validates correctly
- [ ] KiiBank form validates correctly
- [ ] All forms submit correctly

### Payment Processing
- [ ] Payment form displays correctly
- [ ] Card number formatting works
- [ ] Payment method selection works
- [ ] Payment processing completes
- [ ] Success page displays correctly

### Transaction Tables
- [ ] Tables are responsive
- [ ] Filters work correctly
- [ ] Pagination works
- [ ] Export button (placeholder) visible
- [ ] Empty states display correctly

## Performance Testing

### Page Load Times
- Landing page: < 2 seconds
- Transaction forms: < 1.5 seconds
- Transaction tables: < 2 seconds (with data)

### JavaScript Performance
- Currency calculator: < 100ms response
- Form validation: < 50ms
- API calls: < 500ms

## Accessibility Testing

### WCAG 2.1 Level AA Compliance
- [ ] Keyboard navigation works
- [ ] Screen reader compatible
- [ ] Color contrast ratios meet standards
- [ ] Form labels are properly associated
- [ ] Error messages are accessible

### Tools
- WAVE (Web Accessibility Evaluation Tool)
- axe DevTools
- Lighthouse Accessibility Audit

## Security Testing

### Payment Security
- [ ] Card details are not logged
- [ ] HTTPS is enforced
- [ ] CSRF tokens are validated
- [ ] Input validation on all forms

### API Security
- [ ] CORS is properly configured
- [ ] API endpoints require authentication (when implemented)
- [ ] SQL injection prevention (EF Core parameterized queries)

## Browser Console Testing

### Check for Errors
1. Open browser DevTools (F12)
2. Check Console tab for errors
3. Check Network tab for failed requests
4. Check Application tab for storage issues

### Common Issues to Check
- JavaScript errors
- 404 errors for assets
- CORS errors
- API timeout errors

## Manual Testing Steps

### 1. Landing Page
```
1. Navigate to home page
2. Select sending country
3. Select receiving country
4. Enter amount
5. Verify exchange rate updates
6. Select transfer method
7. Click Transfer button
8. Verify redirect to transaction form
```

### 2. Bank Deposit Flow
```
1. Complete bank deposit form
2. Select bank from dropdown
3. Enter account details
4. Submit form
5. Verify redirect to payment page
6. Complete payment
7. Verify success page
```

### 3. Mobile Transfer Flow
```
1. Complete mobile transfer form
2. Select wallet operator
3. Enter mobile number
4. Submit form
5. Complete payment
6. Verify success
```

### 4. Transaction Tables
```
1. Navigate to transactions page
2. Test filters
3. Test pagination
4. Test responsive layout (resize browser)
5. Test on mobile device
```

## Automated Testing (Future)

### Unit Tests
- Exchange rate calculations
- Fee calculations
- Form validation

### Integration Tests
- API endpoints
- Database operations
- Payment processing

### E2E Tests
- Complete transaction flow
- Form submissions
- Navigation

## Known Issues and Workarounds

### iOS Safari
- **Issue**: Input zoom on focus
- **Workaround**: Font-size 16px on inputs

### IE11
- **Issue**: CSS Grid not supported
- **Workaround**: Flexbox fallback

### Firefox
- **Issue**: Print styles may differ
- **Workaround**: Test print preview

## Testing Tools

### Browser DevTools
- Chrome DevTools
- Firefox Developer Tools
- Safari Web Inspector

### Responsive Testing
- Chrome DevTools Device Mode
- BrowserStack
- Responsive Design Mode

### Performance
- Lighthouse
- WebPageTest
- Chrome Performance Profiler

## Test Data

### Test Countries
- Sending: GB (United Kingdom)
- Receiving: NG (Nigeria), GH (Ghana), KE (Kenya)

### Test Amounts
- Minimum: 1.00
- Maximum: 50000.00
- Typical: 100.00, 500.00, 1000.00

### Test Cards (for payment testing)
- Use test card numbers from payment gateway documentation
- Never use real card numbers in testing

## Reporting Issues

When reporting issues, include:
1. Browser and version
2. Device and screen size
3. Steps to reproduce
4. Expected vs actual behavior
5. Console errors (if any)
6. Screenshots

