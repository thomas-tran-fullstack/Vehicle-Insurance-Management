# Demo backend file for Insurance Management
# This file demonstrates the Insurance Management CRUD operations

import requests

BASE_URL = "http://localhost:5169/api/InsuranceManagement"

def demo():
    """Demo Insurance Management API endpoints"""
    
    # 1. Get all insurances
    print("1. Getting all insurances...")
    response = requests.get(f"{BASE_URL}/all")
    print(f"Status: {response.status_code}")
    print(f"Data: {response.json()}\n")
    
    # 2. Create a new insurance
    print("2. Creating a new insurance...")
    new_insurance = {
        "typeCode": "TEST_BASIC",
        "typeName": "Test Insurance Basic",
        "description": "This is a test insurance type",
        "baseRatePercent": 2.50
    }
    response = requests.post(BASE_URL, json=new_insurance)
    print(f"Status: {response.status_code}")
    print(f"Response: {response.json()}\n")
    
    # 3. Get insurance by id
    if response.status_code == 200:
        insurance_id = response.json().get('data', {}).get('insuranceTypeId')
        print(f"3. Getting insurance with ID {insurance_id}...")
        response = requests.get(f"{BASE_URL}/{insurance_id}")
        print(f"Status: {response.status_code}")
        print(f"Data: {response.json()}\n")
        
        # 4. Update insurance
        print(f"4. Updating insurance with ID {insurance_id}...")
        update_data = {
            "typeName": "Test Insurance Basic Updated",
            "description": "Updated description",
            "baseRatePercent": 3.50
        }
        response = requests.put(f"{BASE_URL}/{insurance_id}", json=update_data)
        print(f"Status: {response.status_code}")
        print(f"Response: {response.json()}\n")
        
        # 5. Deactivate insurance
        print(f"5. Deactivating insurance with ID {insurance_id}...")
        response = requests.put(f"{BASE_URL}/{insurance_id}/deactivate")
        print(f"Status: {response.status_code}")
        print(f"Response: {response.json()}\n")
    
    # 6. Search insurances
    print("6. Searching insurances by code...")
    response = requests.get(f"{BASE_URL}/all?search=CAR")
    print(f"Status: {response.status_code}")
    print(f"Data: {response.json()}\n")
    
    # 7. Filter by status
    print("7. Filtering active insurances...")
    response = requests.get(f"{BASE_URL}/all?status=ACTIVE")
    print(f"Status: {response.status_code}")
    print(f"Data: {response.json()}\n")

if __name__ == "__main__":
    demo()
