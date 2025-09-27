Feature: Collaboration
    As a user
    I want to collaborate on tasks
    So that I can work with others effectively

    Background:
        Given I am a registered user
        And there is another registered user

    Scenario: Add a comment to a task
        Given I have created a task
        When I add a comment "This is important" to the task
        Then the comment should be added successfully
        And the comment should appear on the task

    Scenario: View comments on a task
        Given a task has comments
        When I view the task details
        Then I should see all comments on the task
        And each comment should have author and timestamp

    Scenario: Update a comment
        Given I have added a comment to a task
        When I update the comment to "Updated comment"
        Then the comment should be updated successfully
        And the comment should have the new content

    Scenario: Delete a comment
        Given I have added a comment to a task
        When I delete the comment
        Then the comment should be deleted successfully
        And the comment should no longer appear on the task

    Scenario: View activity log
        Given there have been activities on tasks
        When I request the activity log
        Then I should see a chronological list of activities
        And each activity should have type, timestamp, and details