# Vehicle Information Management - User Guide

## For Customers

### Adding a New Vehicle

1. **Navigate to My Vehicles**
   - Click "My Vehicles" in the navigation menu
   - Click "Add New Vehicle" button

2. **Fill Vehicle Information**
   - **Basic Information:**
     - Vehicle Name (e.g., "Toyota Camry")
     - Vehicle Type (Sedan, SUV, Hatchback, etc.)
     - Brand (Toyota, Honda, etc.)
     - Model (Camry, CR-V, etc.)
     - Segment (A, B, C, D, E, F, or J)
     - Version (optional, e.g., "2.5L Automatic")

   - **Identification & Registration:**
     - Registration Number/Plate (must be unique, e.g., "29A-12345")
     - Manufacture Year
     - Body Number/Chassis (must be unique)
     - Engine Number (must be unique)
     - Registration Date
     - Number of Seats

   - **Insurance Information:**
     - Vehicle Rate (used for premium calculation)

   - **Vehicle Image (Optional)**
     - Click to upload or drag-and-drop image
     - Supports PNG, JPG, GIF (max 10MB)

3. **Submit**
   - Click "Add Vehicle" button
   - You'll see success notification and redirect to vehicle list

### Editing a Vehicle

1. **From Vehicle List**
   - Find the vehicle you want to edit
   - Click the edit icon (pencil)

2. **Update Information**
   - Modify any fields you need
   - You can change the vehicle image if needed
   - Click "Save Changes"

3. **Confirmation**
   - Success message appears
   - Redirected to vehicle list

### Deleting a Vehicle

1. **From Vehicle List**
   - Find the vehicle to delete
   - Click the delete icon (trash)

2. **Confirm Deletion**
   - Modal dialog appears asking for confirmation
   - Click "Delete" to confirm
   - The vehicle is permanently removed

### Viewing Your Vehicles

**Vehicle List Features:**
- **Search:** Find vehicles by name, brand, model, or plate
- **Filters:**
  - By Brand (Toyota, Honda, etc.)
  - By Segment (A, B, C, D, E, F, J)
- **Sort:** Vehicles are sorted by newest first

**Stats Cards:**
- Total number of vehicles
- Count by type (Sedan, SUV, Other)

**Actions for Each Vehicle:**
- Edit (pencil icon) - Update information
- Delete (trash icon) - Remove vehicle
- Get Quote (document icon) - Request insurance estimate

### Getting an Insurance Quote

1. From vehicle list, click "Get Quote" button on any vehicle
2. You'll be taken to the estimate request form
3. Vehicle information will be pre-filled
4. Complete the estimation form to get a quote

---

## For Administrators

### Vehicle Management Dashboard

1. **Navigate to Vehicle Management**
   - Access from admin sidebar

2. **Dashboard Features:**
   - **Stats Summary:**
     - Total Vehicles in system
     - Active Policies count
     - Pending Inspections

### Searching Vehicles

**Search Box:**
- Search by:
  - License plate (e.g., "29A")
  - Body/Chassis number
  - Engine number
  - Customer name
  - Vehicle name

**Advanced Filters:**
1. **Brand Filter:**
   - Dynamically populated from all vehicles in system
   - Select to filter by specific brand

2. **Segment Filter:**
   - A (Micro Cars)
   - B (Small Cars)
   - C (Compact Cars)
   - D (Mid-Size Cars)
   - E (Large Cars)
   - F (Luxury Cars)
   - J (SUVs)

3. **Seat Count Filter:**
   - 2, 4, 5, 7, 9+ seats

4. **Reset Button:**
   - Clear all filters and search
   - Returns to full vehicle list

### Viewing Vehicle Details

1. **From Vehicle List:**
   - Locate the vehicle
   - Click the info icon (⊕) in Actions column

2. **Detail Page Shows:**

   **Section 1: Driver Information**
   - Customer/Owner name
   - Email address
   - Phone number
   - Address
   - Customer ID
   - Profile avatar (if available)

   **Section 2: Vehicle Information**
   - Vehicle image (or placeholder)
   - Full vehicle specifications:
     - Name, type, brand, model
     - Segment, version
     - License plate, body number, engine number
     - Manufacture year, seat count
     - Insurance rate
     - Registration date
     - Added date to system

   **Section 3: Policy & Insurance Information**
   - List of all insurance policies linked to vehicle
   - For each policy:
     - Policy number
     - Start and end dates
     - Policy status (Active, Expired, etc.)
     - Premium amount
     - Coverage amount
   - Message if no policies are linked

### Exporting Vehicle List

1. **Export Button:**
   - Located in top-right of vehicle management table
   - Click "Export List" button

2. **File Generated:**
   - CSV file downloaded to your computer
   - Filename: `vehicles_YYYY-MM-DD.csv`
   - Includes only currently filtered/searched vehicles

3. **CSV Contents:**
   - Vehicle ID
   - Name
   - Type
   - Brand
   - Segment
   - License Plate
   - Owner Name
   - Seat Count
   - Insurance Rate

### Bulk Operations

**Future Enhancements (Coming Soon):**
- Bulk export
- Bulk status updates
- Batch operations

---

## Common Workflows

### Workflow 1: Customer Adds New Vehicle

Customer Flow:
```
Homepage → My Vehicles → Add New Vehicle → Fill Form → 
Vehicle Added → Confirmation → Vehicle List
```

### Workflow 2: Admin Views Customer's Vehicle Details

Admin Flow:
```
Admin Dashboard → Vehicle Management → Search/Filter → 
Click Detail Icon → View All Information (3 Sections)
```

### Workflow 3: Check Insurance Coverage

Admin Flow:
```
Vehicle Management → Find Vehicle → Click Detail → 
Scroll to "Policy & Insurance" section → View active policies
```

### Workflow 4: Export Vehicle Report

Admin Flow:
```
Vehicle Management → Apply Filters (Optional) → 
Click "Export List" → CSV file downloads
```

---

## Tips & Troubleshooting

### Tips for Customers

1. **Unique Numbers:**
   - Ensure you enter correct body and engine numbers
   - They must be unique in the system
   - If you get error "already exists", double-check the number

2. **Vehicle Image:**
   - Optional but recommended for identification
   - Clear, well-lit photos work best
   - Try drag-and-drop if click upload doesn't work

3. **Finding Your Vehicle:**
   - Use search and filters to quickly locate vehicles
   - Brand filter is especially useful for large lists

4. **Getting Quotes:**
   - Vehicle must be added before requesting insurance quote
   - You can request multiple quotes for same vehicle

### Tips for Admins

1. **Search Efficiency:**
   - Use partial plate numbers (e.g., "29A" instead of "29A-12345")
   - Search by customer name to find all their vehicles
   - Segment filter is useful for targeting vehicle classes

2. **Bulk Exports:**
   - Apply filters before exporting to get specific data
   - Use exported CSV for analysis in Excel/Google Sheets

3. **Monitoring:**
   - Check "Pending Inspections" stat regularly
   - Use detail view to see policy status at a glance
   - Identify vehicles without insurance coverage

### Troubleshooting

**Q: "Vehicle with this...already exists" error**
- A: The body number, engine number, or plate is already in use
- Solution: Verify the number is correct and unique

**Q: Image won't upload**
- A: File might be too large or unsupported format
- Solution: Use PNG, JPG, or GIF files under 10MB

**Q: Can't see other customers' vehicles (Customer)**
- A: This is correct - you can only see your own vehicles
- Solution: Contact admin if you need to see a different vehicle

**Q: Search returns no results**
- A: Try:
  - Partial search terms (e.g., "Toyota" instead of exact match)
  - Clear filters to see all vehicles
  - Check spelling of search term

---

## Security Notes

- Your password is never stored in plain text
- Vehicle data is encrypted in database
- Only vehicle owners can edit/delete their vehicles
- Admin can view all vehicles but cannot modify without proper authorization
- All API calls require JWT authentication token
- Image files are stored securely on server

---

## Support

For issues or questions:
1. Check this guide for common workflows
2. Contact administrator for account/permission issues
3. Report bugs with specific error messages and steps to reproduce
