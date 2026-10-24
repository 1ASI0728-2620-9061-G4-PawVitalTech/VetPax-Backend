@medication_e2e
Feature: Medication Treatment API
As a VetPax user
I want secure medication treatment endpoints
So that prescriptions and reminders are managed correctly

Scenario: US07 - Owner enables medication reminders
    Given the VetPax Medication E2E environment is configured
    And the test veterinarian creates a medication plan
    When the test owner sets medication reminders to "true"
    Then the medication response status is 200
    And medication reminders are "true" in the response

Scenario: US07 - Owner disables medication reminders
    Given the VetPax Medication E2E environment is configured
    And the test veterinarian creates a medication plan
    When the test owner sets medication reminders to "false"
    Then the medication response status is 200
    And medication reminders are "false" in the response

Scenario: TS05 - Unauthenticated users cannot read treatments
    Given the VetPax Medication E2E environment is configured
    When a user without JWT requests a medication plan
    Then the medication response status is 401

Scenario: TS05 - An owner cannot create prescriptions
    Given the VetPax Medication E2E environment is configured
    When the test owner attempts to create a medication plan
    Then the medication response status is 403

Scenario: US18 - Administration timestamp is required
    Given the VetPax Medication E2E environment is configured
    When the test owner registers a dose without a timestamp
    Then the medication response status is 400