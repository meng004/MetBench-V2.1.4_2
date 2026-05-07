import re
from sympy import latex, sympify, evaluate
from sympy.parsing.sympy_parser import parse_expr

def convert_to_latex(expression):
    """
    将输入/输出模式表达式转换为符合规范的LaTeX格式，严格保持原始顺序

    Args:
        expression (str): 输入的数学表达式，如 "x2,1 = sin(π) + x1,1"

    Returns:
        tuple: (input_pattern, output_pattern) 符合规范的LaTeX格式
    """

    def process_single_expression(expr, is_output=False):
        # 分割等式左右两边
        parts = [p.strip() for p in expr.split("=", 1)]
        if len(parts) != 2:
            raise ValueError("表达式必须包含一个等号")

        lhs, rhs = parts

        # # 处理左侧变量
        # if not re.match(r'^[xy]\d+,\d+$', lhs):
        #     raise ValueError(f"左侧变量格式不正确: {lhs}")
        #
        # # 转换变量格式：x2,1 → x_{21}
        # lhs_latex = re.sub(r'([xy])(\d+),(\d+)', r'\1_{\2\3}', lhs)
        # 处理左侧变量（支持带系数的情况）
        lhs_coeff = None
        lhs_var = lhs

        # 匹配系数模式（如 1.0*y2,1）
        coeff_match = re.match(r'^([\d\.]+)\s*\*\s*([xy]\d+,\d+)$', lhs)
        if coeff_match:
            lhs_coeff = float(coeff_match.group(1))
            lhs_var = coeff_match.group(2)

        # 验证变量格式
        if not re.match(r'^[xy]\d+,\d+$', lhs_var):
            raise ValueError(f"左侧变量格式不正确: {lhs}")

        # 转换变量格式：x2,1 → x_{21}
        lhs_latex = re.sub(r'([xy])(\d+),(\d+)', r'\1_{\2\3}', lhs_var)

        # 如果是输出模式，将x替换为y
        if is_output:
            lhs_latex = lhs_latex.replace("x", "y")

        # 处理右侧表达式
        # 1. 替换Unicode符号
        rhs_processed = rhs.replace("π", "pi")
        # 2. 转换变量格式但保留顺序标记
        rhs_processed = re.sub(r'([xy])(\d+),(\d+)', r'\1@\2@\3', rhs_processed)

        try:
            # 3. 禁用自动排序并解析
            with evaluate(False):
                rhs_expr = parse_expr(rhs_processed.replace("@", "_"), evaluate=False)
                # 4. 转换为LaTeX
                rhs_latex = latex(rhs_expr, mul_symbol='*')  # 强制保留 *
                # 5. 恢复变量格式（确保无空格）
                rhs_latex = re.sub(r'([xy])_\{?(\d+)\}?[ _]\{?(\d+)\}?', r'\1_{\2\3}', rhs_latex)
                # 自动去除1.0*（仅在左侧系数为1.0时）
                if lhs_coeff is not None and 0.999 <= lhs_coeff <= 1.001:
                    rhs_latex = re.sub(r'1\.0\s*\\cdot\s*', '', rhs_latex)
                # 6. 如果是输出模式，右侧的x也要替换成y
                if is_output:
                    rhs_latex = rhs_latex.replace("x_{", "y_{")
        except Exception as e:
            raise ValueError(f"无法解析右侧表达式: {rhs} ({str(e)})")

        return f"{lhs_latex}={rhs_latex}"

    # 处理多表达式情况
    expressions = [e.strip() for e in expression.split(", ") if e.strip()]
    if len(expressions) > 1:
        input_parts = []
        # output_parts = []
        for expr in expressions:
            if "=" not in expr:
                continue
            input_parts.append(process_single_expression(expr, False))
            # output_parts.append(process_single_expression(expr, True))
        input_pattern = ", ".join(input_parts)
        # output_pattern = ", ".join(output_parts)
    else:
        input_pattern = process_single_expression(expression, False)
        # output_pattern = process_single_expression(expression, True)

    return input_pattern


# 测试用例
if __name__ == "__main__":
    test_cases = [
        "x2,1 = 9.42477796076938 - 1.0*x1,1",  # 传统无系数格式
        "1.0*y2,1 = 1.0*y1,1",  # 带1.0系数格式
        "y2,1 = 2.5*y1,1 + 3.0*y3,1",  # 右侧含非1系数
        "1.0*x1,1 = sin(pi) + 1.0*x2,1"  # 左侧1.0系数+复杂右侧
    ]

    for expr in test_cases:
        print(f"输入表达式: {expr}")
        try:
            input_latex= convert_to_latex(expr)
            print(f"输入模式: {input_latex}")
        except Exception as e:
            print(f"错误: {e}\n")