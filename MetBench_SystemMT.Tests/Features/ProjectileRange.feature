Feature: Projectile range MT - doubling initial speed quadruples the range

  Background:
    The closed-form projectile range is R = v0^2 * sin(2*theta) / g.
    Doubling v0 with theta and g held constant must increase the range,
    which is the GreaterThan MR validated below.

  Scenario: Follow-up output range is greater than source output range
    Given a projectile MT case named "source" with input "10 45 9.81"
    And a projectile MT case named "follow-up" with input "20 45 9.81"
    When I run both projectile cases
    Then the projectile parsed value "range" of "follow-up" should exceed "source"
    And the follow-up range should be approximately 4 times the source range
