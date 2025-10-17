# Quick Start Guide - New Reveal Studio UI

## What's New? 🎉

Your Dynamic Query Builder now has a beautiful, professional interface inspired by modern design patterns!

### Key Features

✅ **Elegant Grid Layout** - View all queries in beautiful cards  
✅ **Collapsible Sidebar** - More screen space when you need it  
✅ **In-Place Navigation** - View/edit dashboards without leaving the page  
✅ **Professional Design** - Clean, modern, and responsive  
✅ **Smooth Transitions** - Beautiful animations throughout  

## Getting Started

### 1. Files to Use

**Main Application**: `client/index-new.html`  
**Dashboard Viewer**: `client/load-dashboard-new.html`  
**Dashboard Editor**: `client/create-dashboard-new.html`  
**Stylesheet**: `client/styles/main.css`  
**JavaScript**: `client/scripts/main.js`  

### 2. Start the Server

```bash
cd server/aspnet/RevealSdk.Server
dotnet run
```

### 3. Open the Application

Navigate to: `http://localhost:[YOUR_PORT]/client/index-new.html`

## User Flow

### Creating a Query
1. Click **"Create New"** button (sidebar) or **"Create Query"** (top right)
2. Redirects to `query-builder.html` (your existing builder)
3. Build and save your query
4. Returns to grid view

### Viewing a Dashboard
1. Find your query in the grid
2. Click **"Show Grid"** button
3. Dashboard opens in viewer mode (read-only)
4. Click **"← Back to Queries"** to return

### Editing a Dashboard
1. Find your query in the grid
2. Click **"Edit Dashboard"** button
3. Dashboard opens in editor mode (full Reveal SDK)
4. Make your changes
5. Click **"← Back to Queries"** to return

## Customization Tips

### Change the Purple Color
In `client/styles/main.css`, find and replace:
- `#6A44FF` (primary)
- `#8A6DFF` (light/gradient)
- `#5838d1` (dark/hover)

### Adjust Card Size
In `client/styles/main.css`, line ~450:
```css
.queries-grid {
    grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
    /* Change 320px to your preferred minimum card width */
}
```

### Sidebar Width
In `client/styles/main.css`, line ~50:
```css
.sidebar {
    width: 256px; /* Change this value */
}
```

## API Integration

Your JavaScript expects a `/queries` endpoint that returns:

```json
[
  {
    "guid": "abc-123",
    "name": "Sales Report",
    "description": "Monthly sales data",
    "sql": "SELECT * FROM sales WHERE...",
    "createdDate": "2025-10-16T10:30:00Z"
  }
]
```

Update `API_BASE_URL` in `client/scripts/main.js` if your server runs on a different port.

## Troubleshooting

### Queries Not Loading?
- Check console for API errors
- Verify server is running on correct port
- Update `API_BASE_URL` in `main.js`

### Dashboard Not Displaying?
- Check browser console for errors
- Verify Reveal SDK scripts are loading
- Check GUID is being passed correctly in URL

### Sidebar Not Working?
- Ensure `main.js` is loaded
- Check for JavaScript errors in console

## Next Steps

1. **Test with Real Data**: Connect to your actual API
2. **Add Features**: Delete queries, rename, etc.
3. **Customize Colors**: Match your brand
4. **Deploy**: Move to production environment

## Support

For issues or questions:
- Check browser console for errors
- Review `NEW_UI_README.md` for detailed documentation
- Review `UI_COMPONENTS.md` for design details

---

**Enjoy your new beautiful UI! 🚀**
