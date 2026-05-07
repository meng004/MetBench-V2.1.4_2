def arctan(x, n_terms=100):
    arctan_x = 0
    if abs(x) > 1:
        return "Invalid input. x should be between -1 and 1."
    for n in range(n_terms):
        coefficient = (-1) ** n
        numerator = x ** (2 * n + 1)
        denominator = 2 * n + 1
        arctan_x += coefficient * (numerator / denominator)
    return arctan_x
