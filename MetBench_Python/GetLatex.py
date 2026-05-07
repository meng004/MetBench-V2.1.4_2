import re
from sympy import latex, sympify,evaluate
from sympy.parsing.sympy_parser import parse_expr


def convert_to_latex(expression):
    """
    将数学表达式转换为LaTeX格式，严格保持原始顺序

    Args:
        expression (str): 输入的数学表达式，如 "x1,1 = sin(π) + x2,1"

    Returns:
        str: 转换后的LaTeX表达式
    """

    def process_expression(expr):
        # 分割等式左右两边
        parts = [p.strip() for p in expr.split("=", 1)]
        if len(parts) != 2:
            raise ValueError("表达式必须包含一个等号")

        lhs, rhs = parts

        # 处理左侧变量格式：x1,1 → x_{11}
        lhs_latex = re.sub(r'([xy])(\d+),(\d+)', r'\1_{\2\3}', lhs)

        # 处理右侧表达式
        # 1. 替换Unicode符号
        rhs_processed = rhs.replace("π", "pi")
        # 2. 转换变量格式：x2,1 → x_2_1（临时格式）
        rhs_processed = re.sub(r'([xy])(\d+),(\d+)', r'\1_\2_\3', rhs_processed)

        try:
            # 3. 禁用自动排序并解析
            with evaluate(False):
                rhs_expr = parse_expr(rhs_processed, evaluate=False)
                # 4. 转换为LaTeX
                rhs_latex = latex(rhs_expr)
                # 5. 恢复变量格式：x_2_1 → x_{2 1}
                rhs_latex = re.sub(r'([xy])_(\d+)_(\d+)', r'\1_{\2 \3}', rhs_latex)
        except Exception as e:
            raise ValueError(f"无法解析右侧表达式: {rhs} ({str(e)})")

        return f"{lhs_latex}={rhs_latex}"

    # 处理多表达式情况
    expressions = [e.strip() for e in expression.split(", ") if e.strip()]
    if len(expressions) > 1:
        return ", ".join([process_expression(e) for e in expressions if "=" in e])
    else:
        return process_expression(expression)


# 测试用例
if __name__ == "__main__":
    test_cases = [
        "x1,1 = sin(π) + x2,1",
        "x2,1 = x1,1 + 2*x1,2",
        "y3,2 = π/2 + cos(π/3)",
        "x1,1 = x2,1 + sin(π) + x3,1",
        "x1,1 = sin(π) + x2,1, x1,2 = x2,2"
    ]

    for expr in test_cases:
        print(f"输入表达式: {expr}")
        try:
            result = convert_to_latex(expr)
            print(f"输出结果: {result}\n")
        except Exception as e:
            print(f"错误: {e}\n")