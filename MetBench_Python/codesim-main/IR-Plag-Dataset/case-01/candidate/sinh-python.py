def factorial(n):
    if n == 0:
        return 1
    else:
        return n * factorial(n-1)
def sinh(x, n_terms=20):
    result = 0
    for n in range(n_terms):
        term = x ** (2*n + 1) / factorial(2*n + 1)
        result += term
    return result


