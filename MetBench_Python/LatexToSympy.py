#这个是latex转sympy
import sys
import re
from sympy import simplify
from sympy.parsing.latex import parse_latex
from sympy import symbols

latex_expression = sys.argv[1]
# latex_expression = r"x_{21} = x_{11}";
separator = ", "
def LatexToSympy(latex_expression):
    sympy_results = []
    symbol_names = re.findall(r"\\[a-zA-Z0-9]+", latex_expression)  # 提取符号变量名称
    symbols_list = [symbols(name) for name in symbol_names]  # 按顺序创建符号变量

    equation_list = latex_expression.split(", ")
    for equation in equation_list:
        sympy_result = parse_latex(equation)
        sympy_results.append(sympy_result)

    sympy_expression_str = separator.join(str(expr) for expr in sympy_results)
    return sympy_expression_str

# 测试
# latex_expression = r"y_{1}+y_{2}+y_{3}-y_{4}=4*y_{5}*y_{6}*y_{7}"
sympy_expression = LatexToSympy(latex_expression)

print(sympy_expression)
# 测试
# X_{1}=X_{2}^{ab}-Y_{1}/(X_{2}*X_{3})+\sin x, X_{2}=\sqrt[n]{x}+\int_{a}^{b}f(x)\,dx, X_{3}=\pi
# 多元表达式
# latex_expression = r"x_2=2*\pi+x_1"
# latex_expression = r"x_4=x_1+x_2+x_3,x_5=x_1/2+x_2/2,x_6=x_1/2+x_3/2,x_7=x_2/2+x_3/2"