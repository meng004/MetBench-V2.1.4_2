Feature: System-level metamorphic testing through CLI

  Scenario: Follow-up output value is greater than source output value
    Given a system MT case named "source" with input file "source-input.txt"
    And a system MT case named "follow-up" with input file "followup-input.txt"
    When I run both cases with program profile "example-cli"
    Then the parsed output value "result" of "follow-up" should be greater than "source"
