import math

def number_of_divisors(n: int) -> int:
    """Calculate Number of Divisors of an Integer.
    # >>> number_of_divisors(100)
    # 9
    # >>> number_of_divisors(0)
    # Traceback (most recent call last):
    #     ...
    # ValueError: Only positive numbers are accepted
    # >>> number_of_divisors(-10)
    Traceback (most recent call last):
        ...
    ValueError: Only positive numbers are accepted
    """
    if n <= 0:
        raise ValueError("Only positive numbers are accepted")
    div = 1
    temp = 1
    while n % 2 == 0:
        temp += 1
        n = int(n / 2)
    div *= temp
    for i in range(3, int(math.sqrt(n)) + 1, 2):
        temp = 1
        while n % i == 0:
            temp += 1
            n = int(n / i)
        div *= temp
    if n > 1:
        div *= 2
    return div