# Vehicle Information Management - Testing Guide

## Application Status
✅ **Build Status:** Successful
✅ **Runtime Status:** Running on `http://localhost:5169`

## Pre-Testing Requirements

### 1. Create Upload Directory
```powershell
# Create directory if it doesn't exist
New-Item -Path "wwwroot/uploads/vehicles" -ItemType Directory -Force
```

### 2. Apply Database Migration
Run Entity Framework migration to update database schema:
```powershell
# Add migration for new Vehicle fields
Add-Migration UpdateVehicleTableWithNewFields

# Apply migration
Update-Database
```

### 3. Ensure JWT Authentication is Configured
The application requires valid JWT tokens. Verify in `Program.cs`:
- `AddAuthentication()` middleware is configured
- `AddJwtBearer()` is set up
- `UseAuthentication()` is called in middleware pipeline

## API Endpoint Testing

### Test Setup
Use Postman, Insomnia, or VS Code Rest Client (VehicleInsuranceAPI.http)

**Base URL:** `http://localhost:5169/api/vehicleinformation`

**Headers (for all requests):**
```
Content-Type: application/json
Authorization: Bearer {YOUR_JWT_TOKEN}
```

---

## 1. Get Customer Vehicles
**Endpoint:** `GET /api/vehicleinformation/customer`

**Expected Behavior:**
- Returns only vehicles owned by authenticated customer
- Shows all vehicle fields (name, brand, segment, type, etc.)
- Ordered by most recent first

**Test Cases:**

✅ **Test 1.1: Retrieve Customer Vehicles (No filter)**
```
GET /customer
Header: Authorization: Bearer {valid_customer_token}
```
Expected Response (200 OK):
```json
{
  "success": true,
  "data": [
    {
      "vehicleId": 1,
      "vehicleName": "Toyota Camry 2023",
      "vehicleType": "Sedan",
      "vehicleBrand": "Toyota",
      "vehicleSegment": "D",
      "vehicleVersion": "2.5L Hybrid",
      "vehicleRate": 1500,
      "bodyNumber": "BD123456789",
      "engineNumber": "ENG123456",
      "vehicleNumber": "AA123BB",
      "registrationDate": "2023-01-15",
      "seatCount": 5,
      "vehicleImage": "/uploads/vehicles/vehicle_1_20260204_143022_camry.jpg",
      "manufactureYear": 2023,
      "modelName": "Camry",
      "createdDate": "2026-02-04T10:30:00",
      "updatedDate": "2026-02-04T10:30:00"
    }
  ]
}
```

❌ **Test 1.2: Without Authentication**
```
GET /customer
(No Authorization header)
```
Expected Response (401 Unauthorized):
```json
{
  "success": false,
  "message": "User not authenticated"
}
```

---

## 2. Get All Vehicles (Admin)
**Endpoint:** `GET /api/vehicleinformation/all`

**Query Parameters:**
- `brand` (optional): Filter by vehicle brand
- `segment` (optional): Filter by vehicle segment
- `seatCount` (optional): Filter by number of seats
- `search` (optional): Search by vehicle name, plate, body#, engine#, or customer name

**Test Cases:**

✅ **Test 2.1: Get All Vehicles (No filter)**
```
GET /all
```

✅ **Test 2.2: Filter by Brand**
```
GET /all?brand=Toyota
```
Expected: Returns only Toyota vehicles

✅ **Test 2.3: Filter by Segment**
```
GET /all?segment=D
```
Expected: Returns only D-segment vehicles

✅ **Test 2.4: Filter by Seat Count**
```
GET /all?seatCount=5
```
Expected: Returns only 5-seater vehicles

✅ **Test 2.5: Search by Vehicle Name**
```
GET /all?search=Camry
```
Expected: Returns vehicles with "Camry" in name

✅ **Test 2.6: Search by License Plate**
```
GET /all?search=AA123BB
```
Expected: Returns vehicles matching plate

✅ **Test 2.7: Search by Customer Name**
```
GET /all?search=John
```
Expected: Returns vehicles owned by customers with "John" in name

✅ **Test 2.8: Combined Filters**
```
GET /all?brand=Toyota&segment=D&search=Camry
```
Expected: Returns Toyota D-segment vehicles with "Camry" in name

---

## 3. Get Vehicle Detail
**Endpoint:** `GET /api/vehicleinformation/{id}`

**Path Parameters:**
- `id`: Vehicle ID (integer)

**Test Cases:**

✅ **Test 3.1: Get Detail of Existing Vehicle**
```
GET /1
```
Expected Response (200 OK):
```json
{
  "success": true,
  "data": {
    "vehicleId": 1,
    "vehicleName": "Toyota Camry 2023",
    "vehicleType": "Sedan",
    "vehicleBrand": "Toyota",
    "vehicleSegment": "D",
    "vehicleVersion": "2.5L Hybrid",
    "vehicleRate": 1500,
    "bodyNumber": "BD123456789",
    "engineNumber": "ENG123456",
    "vehicleNumber": "AA123BB",
    "registrationDate": "2023-01-15",
    "seatCount": 5,
    "vehicleImage": "/uploads/vehicles/vehicle_1_20260204_143022_camry.jpg",
    "manufactureYear": 2023,
    "modelName": "Camry",
    "customer": {
      "customerId": 1,
      "customerName": "John Doe",
      "address": "123 Main St",
      "phone": "0912345678",
      "avatar": "/uploads/avatars/john.jpg",
      "email": "john@example.com"
    },
    "policies": [
      {
        "policyId": 5,
        "policyNumber": 1000005,
        "startDate": "2026-01-01",
        "endDate": "2027-01-01",
        "status": "ACTIVE",
        "premiumAmount": 2500,
        "coverageAmount": 12
      }
    ],
    "createdDate": "2026-02-04T10:30:00",
    "updatedDate": "2026-02-04T10:30:00"
  }
}
```

❌ **Test 3.2: Get Detail of Non-existent Vehicle**
```
GET /99999
```
Expected Response (404 Not Found):
```json
{
  "success": false,
  "message": "Vehicle not found"
}
```

---

## 4. Create Vehicle
**Endpoint:** `POST /api/vehicleinformation`

**Request Type:** `multipart/form-data`

**Form Fields:**
- `vehicleName`: string (required)
- `vehicleType`: string (required)
- `vehicleBrand`: string (required)
- `vehicleSegment`: string (required)
- `vehicleVersion`: string (required)
- `vehicleRate`: decimal
- `bodyNumber`: string (required, unique)
- `engineNumber`: string (required, unique)
- `vehicleNumber`: string (required, unique)
- `registrationDate`: datetime (optional)
- `seatCount`: int (optional)
- `manufactureYear`: int (optional)
- `vehicleImageFile`: file (optional, jpg/png)

**Test Cases:**

✅ **Test 4.1: Create Vehicle with All Fields**
```
POST /
Content-Type: multipart/form-data

vehicleName: Toyota Camry 2023
vehicleType: Sedan
vehicleBrand: Toyota
vehicleSegment: D
vehicleVersion: 2.5L Hybrid
vehicleRate: 1500
bodyNumber: BD987654321
engineNumber: ENG987654
vehicleNumber: CC456DD
registrationDate: 2023-01-15
seatCount: 5
manufactureYear: 2023
vehicleImageFile: (image file)
```
Expected Response (201 Created):
```json
{
  "success": true,
  "message": "Vehicle created successfully",
  "data": {
    "vehicleId": 2,
    "vehicleName": "Toyota Camry 2023",
    ...
  }
}
```

✅ **Test 4.2: Create Vehicle Without Image**
```
POST /
(Same fields as 4.1 but no vehicleImageFile)
```
Expected Response: (201 Created) - Vehicle created, VehicleImage will be null

❌ **Test 4.3: Missing Required Field (VehicleName)**
```
POST /
(All fields except vehicleName)
```
Expected Response (400 Bad Request):
```json
{
  "success": false,
  "message": "Vehicle name and number are required"
}
```

❌ **Test 4.4: Duplicate Body Number**
```
POST /
vehicleName: Another Car
vehicleType: SUV
vehicleBrand: Honda
vehicleSegment: C
vehicleVersion: 2.0L
bodyNumber: BD987654321  (Same as Test 4.1)
...
```
Expected Response (400 Bad Request):
```json
{
  "success": false,
  "message": "Vehicle with this body number, engine number, or registration number already exists"
}
```

❌ **Test 4.5: Duplicate License Plate**
```
POST /
(Change only vehicleNumber to same as existing)
```
Expected Response (400 Bad Request): Same as 4.4

❌ **Test 4.6: Duplicate Engine Number**
```
POST /
(Change only engineNumber to same as existing)
```
Expected Response (400 Bad Request): Same as 4.4

---

## 5. Update Vehicle
**Endpoint:** `PUT /api/vehicleinformation/{id}`

**Request Type:** `multipart/form-data`

**Test Cases:**

✅ **Test 5.1: Update Vehicle Details**
```
PUT /1
Content-Type: multipart/form-data

vehicleName: Toyota Camry 2024 (Updated)
vehicleType: Sedan
vehicleBrand: Toyota
vehicleSegment: D
vehicleVersion: 2.5L Hybrid
vehicleRate: 1600
bodyNumber: BD123456789
engineNumber: ENG123456
vehicleNumber: AA123BB
seatCount: 5
```
Expected Response (200 OK):
```json
{
  "success": true,
  "message": "Vehicle updated successfully",
  "data": {
    "vehicleId": 1,
    "vehicleName": "Toyota Camry 2024 (Updated)",
    ...
  }
}
```

✅ **Test 5.2: Replace Vehicle Image**
```
PUT /1
(Same fields as 5.1 + new vehicleImageFile)
```
Expected Response (200 OK) - Old image deleted, new image stored

❌ **Test 5.3: Update Non-existent Vehicle**
```
PUT /99999
(Any valid data)
```
Expected Response (404 Not Found):
```json
{
  "success": false,
  "message": "Vehicle not found"
}
```

❌ **Test 5.4: Update with Duplicate Body Number**
```
PUT /1
(Change bodyNumber to another existing vehicle's bodyNumber)
```
Expected Response (400 Bad Request): Unique constraint violation

---

## 6. Delete Vehicle
**Endpoint:** `DELETE /api/vehicleinformation/{id}`

**Test Cases:**

✅ **Test 6.1: Delete Existing Vehicle**
```
DELETE /2
```
Expected Response (200 OK):
```json
{
  "success": true,
  "message": "Vehicle deleted successfully"
}
```

✅ **Test 6.2: Verify Image Deletion**
- After Test 6.1, verify that the image file in `/uploads/vehicles/` was deleted

❌ **Test 6.3: Delete Non-existent Vehicle**
```
DELETE /99999
```
Expected Response (404 Not Found):
```json
{
  "success": false,
  "message": "Vehicle not found"
}
```

---

## Frontend Testing

### Test Setup
1. Ensure application is running on `http://localhost:5169`
2. Open browser and navigate to application URL
3. Login as a customer or admin user

---

## 7. Customer Vehicle List (VehiclesList.html)

✅ **Test 7.1: View Vehicle List**
- Navigate to Vehicle List page
- Verify all customer's vehicles are displayed in grid format
- Check that stats (Total, Sedan, SUV, Other) are calculated correctly

✅ **Test 7.2: Search by Vehicle Name**
- Enter "Camry" in search box
- Verify only vehicles with "Camry" in name are shown

✅ **Test 7.3: Search by License Plate**
- Enter "AA123" in search box
- Verify only vehicles with matching plate are shown

✅ **Test 7.4: Filter by Brand**
- Click Brand filter dropdown
- Select "Toyota"
- Verify only Toyota vehicles are shown
- Verify dropdown updates dynamically based on vehicles

✅ **Test 7.5: Filter by Segment**
- Click Segment filter dropdown
- Select "D"
- Verify only D-segment vehicles are shown

✅ **Test 7.6: Combined Filters**
- Select Brand = "Toyota" AND Segment = "D"
- Verify intersection of filters is displayed

✅ **Test 7.7: Reset Filters**
- Apply various filters
- Click "Reset Filters" button
- Verify all vehicles are shown again and dropdowns are cleared

✅ **Test 7.8: Edit Vehicle**
- Click "Edit" button on any vehicle
- Verify redirect to VehicleEdit.html with vehicle ID in URL
- Verify form is pre-populated with vehicle data

✅ **Test 7.9: Delete Vehicle with Confirmation**
- Click "Delete" button on any vehicle
- Verify delete confirmation modal appears with vehicle name
- Click "Confirm Delete"
- Verify vehicle is removed from list and success message appears

✅ **Test 7.10: Cancel Delete**
- Click "Delete" on a vehicle
- Verify modal appears
- Click "Cancel"
- Verify modal closes and vehicle remains in list

✅ **Test 7.11: Get Quote**
- Click "Get Quote" on any vehicle
- Verify navigation to insurance estimate page (or appropriate action)

---

## 8. Add Vehicle (VehicleAdd.html)

✅ **Test 8.1: Fill Form with All Fields**
- Enter all required and optional fields:
  - Vehicle Name: "Honda CR-V"
  - Type: "SUV"
  - Brand: "Honda"
  - Segment: "C"
  - Version: "2.0L Turbo"
  - Rate: 2000
  - Body Number: "BDxxxxxx"
  - Engine Number: "ENGxxxxx"
  - License Plate: "DD777EE"
  - Year: 2024
  - Seats: 7
- Upload vehicle image (drag & drop or click to upload)
- Click "Add Vehicle"
- Verify success message and redirect to VehiclesList

✅ **Test 8.2: Form Validation**
- Try submitting with empty required fields
- Verify error messages appear

✅ **Test 8.3: Duplicate Body Number Error**
- Try creating vehicle with same body number as existing
- Verify error message: "Vehicle with this body number... already exists"

✅ **Test 8.4: Duplicate License Plate Error**
- Try creating vehicle with same plate as existing
- Verify error message

✅ **Test 8.5: Image Upload Validation**
- Try uploading non-image file
- Verify appropriate error message

✅ **Test 8.6: Cancel Form**
- Click "Cancel" button
- Verify redirect to VehiclesList without saving

---

## 9. Edit Vehicle (VehicleEdit.html)

✅ **Test 9.1: Load Vehicle Data**
- Navigate to VehicleEdit.html?id=1
- Verify form loads with skeleton animation
- Verify form pre-populates with vehicle data

✅ **Test 9.2: Edit Vehicle Details**
- Change vehicle name from "Toyota Camry" to "Toyota Camry SE"
- Click "Save Changes"
- Verify success message
- Verify redirect to VehiclesList

✅ **Test 9.3: Replace Vehicle Image**
- Click on current image or image area
- Upload new image
- Click "Save Changes"
- Verify old image is deleted and new image is displayed

✅ **Test 9.4: Duplicate Constraint Check**
- Try changing body number to existing vehicle's body number
- Click "Save Changes"
- Verify error message appears

✅ **Test 9.5: Cancel Edit**
- Make changes to form
- Click "Cancel" button
- Verify redirect to VehiclesList without saving changes

✅ **Test 9.6: Invalid Vehicle ID**
- Navigate to VehicleEdit.html?id=99999
- Verify error message is displayed

---

## 10. Admin Vehicle Management (VehicleManageNew.html)

✅ **Test 10.1: View All Vehicles**
- Navigate to VehicleManage page (admin only)
- Verify table displays all vehicles from all customers

✅ **Test 10.2: View Statistics**
- Verify stat cards show:
  - Total Vehicles count
  - Active Policies count
  - Pending Inspections count

✅ **Test 10.3: Search by License Plate**
- Enter "AA123" in search box
- Verify table filters to show only matching vehicles

✅ **Test 10.4: Search by Body Number**
- Enter "BD123" in search box
- Verify filtering works

✅ **Test 10.5: Search by Engine Number**
- Enter "ENG123" in search box
- Verify filtering works

✅ **Test 10.6: Search by Customer Name**
- Enter "John" in search box
- Verify filtering works

✅ **Test 10.7: Filter by Brand**
- Select "Toyota" from Brand dropdown
- Verify table shows only Toyota vehicles

✅ **Test 10.8: Filter by Segment**
- Select "D" from Segment dropdown
- Verify table shows only D-segment vehicles

✅ **Test 10.9: Filter by Seat Count**
- Select "5" from Seat Count dropdown
- Verify table shows only 5-seater vehicles

✅ **Test 10.10: Reset Filters**
- Apply multiple filters
- Click "Reset Filters"
- Verify all vehicles are shown again

✅ **Test 10.11: View Vehicle Detail**
- Click info icon (detail button) for any vehicle
- Verify redirect to VehicleDetailAdmin.html?id={vehicleId}

✅ **Test 10.12: Export to CSV**
- Click "Export List" button
- Verify CSV file downloads with filename: `vehicles_YYYY-MM-DD.csv`
- Open CSV and verify:
  - Headers: Vehicle, Plate, Owner, Brand, Type, Rate, Year
  - All vehicles are included
  - Data is correctly formatted

---

## 11. Admin Vehicle Detail (VehicleDetailAdmin.html)

✅ **Test 11.1: View Full Detail Page**
- Navigate to VehicleDetailAdmin.html?id=1
- Verify page loads with vehicle information

✅ **Test 11.2: Driver Information Section**
- Verify displays:
  - Customer avatar
  - Customer name
  - Email
  - Phone
  - Address
  - Customer ID

✅ **Test 11.3: Vehicle Information Section**
- Verify displays:
  - Vehicle image
  - Vehicle name
  - Type
  - Brand
  - Model
  - Segment
  - Version
  - License plate
  - Body number
  - Engine number
  - Year
  - Seats
  - Insurance rate
  - Creation/Update dates

✅ **Test 11.4: Policy & Insurance Section (With Policies)**
- For vehicle with policies, verify displays:
  - Policy number
  - Start date
  - End date
  - Status
  - Premium amount
  - Coverage amount (shows as duration in months)
  - Multiple policies listed if applicable

✅ **Test 11.5: Policy & Insurance Section (No Policies)**
- For vehicle without policies, verify displays:
  - Message: "No insurance policies associated with this vehicle"

✅ **Test 11.6: Back Button**
- Click "Back" button
- Verify redirect to VehicleManage page

✅ **Test 11.7: Loading State**
- Verify loading skeleton displays while data is being fetched

✅ **Test 11.8: Invalid Vehicle ID**
- Navigate to VehicleDetailAdmin.html?id=99999
- Verify error message is displayed

---

## Error Handling Tests

✅ **Test E1: Network Error During Vehicle List Load**
- Open browser DevTools Network tab
- Go Offline before loading VehiclesList
- Verify appropriate error message

✅ **Test E2: Server Error (500)**
- Simulate server error in API
- Verify frontend shows "Internal server error" message

✅ **Test E3: Unauthorized Access**
- Remove/invalid JWT token
- Attempt API call
- Verify 401 error and appropriate message

---

## Performance Tests

✅ **Test P1: Load Time - Vehicle List**
- Measure page load time
- Should load in < 2 seconds

✅ **Test P2: Filter Response Time**
- Apply filters
- Should respond within < 500ms

✅ **Test P3: Search Response Time**
- Enter search term
- Should filter within < 300ms

✅ **Test P4: Image Upload Time**
- Upload 5MB image
- Should complete within < 5 seconds

---

## Browser Compatibility Tests

✅ **Test B1: Chrome Latest**
- Test on Chrome latest version
- Verify all features work

✅ **Test B2: Firefox Latest**
- Test on Firefox latest version
- Verify all features work

✅ **Test B3: Edge Latest**
- Test on Edge latest version
- Verify all features work

✅ **Test B4: Mobile Responsive**
- Test on mobile device or use DevTools mobile view
- Verify responsive design works
- Verify touch interactions work

---

## Security Tests

✅ **Test SEC1: SQL Injection Prevention**
- Try entering SQL in search: `' OR '1'='1`
- Verify it's treated as text, not executed

✅ **Test SEC2: XSS Prevention**
- Try entering HTML tags in vehicle name: `<script>alert('XSS')</script>`
- Verify it's escaped/sanitized

✅ **Test SEC3: CSRF Protection**
- Verify CSRF tokens are used for state-changing operations

✅ **Test SEC4: Ownership Verification**
- Verify customer can only see/edit their own vehicles
- Try accessing another customer's vehicle via URL
- Verify access is denied or unauthorized error appears

---

## Test Results Summary Template

```
Test Date: ___________
Tester: _______________
Environment: Development / Staging / Production

PASSED: ____ / ____ tests
FAILED: ____ / ____ tests
BLOCKED: ____ / ____ tests

Critical Issues Found:
- [ ] None
- [ ] (List issues)

Notes:
_________________________________________________________________________

Sign-off:
Tester: ____________    Date: __________
```

---

## Next Steps After Testing

1. **Fix any bugs identified** during testing
2. **Run database migration** if not already done
3. **Deploy to staging environment**
4. **Perform user acceptance testing (UAT)**
5. **Deploy to production**
6. **Monitor production logs** for errors
7. **Gather user feedback** and iterate
