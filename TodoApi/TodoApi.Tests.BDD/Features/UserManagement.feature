Feature: User Management
    As a user
    I want to manage my account
    So that I can access the system securely

    Scenario: Register a new user
        When I register with email "john@example.com" and password "password123"
        Then the user should be registered successfully
        And I should receive a user ID

    Scenario: Login with valid credentials
        Given I have registered with email "jane@example.com" and password "password123"
        When I login with email "jane@example.com" and password "password123"
        Then I should be logged in successfully
        And I should receive an authentication token

    Scenario: Login with invalid credentials
        When I login with email "invalid@example.com" and password "wrongpassword"
        Then the login should fail
        And I should receive an unauthorized error

    Scenario: Update user profile
        Given I am logged in as a registered user
        When I update my profile with name "John Doe"
        Then my profile should be updated successfully
        And the profile should have the new name

    Scenario: Get user profile
        Given I am logged in as a registered user
        When I request my profile
        Then I should receive my profile information
        And the profile should contain my email and name