import math


def exact_prime_factor_count(n: int) -> int:
    """
    # >>> exact_prime_factor_count(51242183)
    3
    """
    count = 0
    if n % 2 == 0:
        count += 1
        while n % 2 == 0:
            n = int(n / 2)
    # the n input value must be odd so that
    # we can skip one element (ie i += 2)

    i = 3

    while i <= int(math.sqrt(n)):
        if n % i == 0:
            count += 1
            while n % i == 0:
                n = int(n / i)
        i = i + 2

    # this condition checks the prime
    # number n is greater than 2

    if n > 2:
        count += 1
    return count
