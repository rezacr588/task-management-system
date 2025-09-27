Feature: Task Management
    As a user
    I want to manage my tasks
    So that I can stay organized and productive

    Background:
        Given I am a registered user

    Scenario: Create a new task
        When I create a task with title "Buy groceries" and description "Weekly shopping"
        Then the task should be created successfully
        And the task should have the correct title and description

    Scenario: Retrieve all tasks
        Given I have created tasks
        When I request all my tasks
        Then I should receive a list of all my tasks

    Scenario: Update a task
        Given I have a task with title "Old task"
        When I update the task title to "Updated task"
        Then the task should be updated successfully
        And the task should have the new title

    Scenario: Mark task as completed
        Given I have an incomplete task
        When I mark the task as completed
        Then the task should be marked as completed

    Scenario: Delete a task
        Given I have a task
        When I delete the task
        Then the task should be deleted successfully
        And the task should no longer exist

    Scenario: Assign tags to a task
        Given I have a task
        And I have created tags "urgent" and "work"
        When I assign the tags to the task
        Then the task should have the assigned tags

    Scenario: Filter tasks by completion status
        Given I have both completed and incomplete tasks
        When I filter tasks by completion status "completed"
        Then I should only see completed tasks