import re
from sympy import latex, sympify, evaluate
from sympy.parsing.sympy_parser import parse_expr
import os
import pandas as pd
import glob
def convert_to_latex(expression):
    """
    增强版LaTeX转换函数，支持处理带系数的表达式
    现在可以处理如 "1.0*y2,1 = 1.0*y1,1" 的格式
    """

    def process_single_expression(expr, is_output=False):
        # 分割等式左右两边
        parts = [p.strip() for p in expr.split("=", 1)]
        if len(parts) != 2:
            raise ValueError("表达式必须包含一个等号")

        lhs, rhs = parts

        # 处理左侧变量（支持带系数的情况）
        lhs_coeff = 1.0
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

        # 添加系数处理（如果系数不是1.0）
        if lhs_coeff != 1.0:
            # 将1.0显示为1，1.5显示为1.5
            coeff_str = str(int(lhs_coeff)) if lhs_coeff.is_integer() else str(lhs_coeff)
            lhs_latex = f"{coeff_str} \cdot {lhs_latex}"

        # 处理右侧表达式
        # 1. 替换Unicode符号
        rhs_processed = rhs.replace("π", "pi")
        # 2. 保留所有*运算符
        rhs_processed = rhs_processed.replace("*", " * ")
        # 3. 转换变量格式但保留顺序标记
        rhs_processed = re.sub(r'([xy])(\d+),(\d+)', r'\1@\2@\3', rhs_processed)

        try:
            # 4. 禁用自动排序并解析
            with evaluate(False):
                rhs_expr = parse_expr(rhs_processed.replace("@", "_"), evaluate=False)
                # 5. 转换为LaTeX（保留*运算符）
                rhs_latex = latex(rhs_expr, mul_symbol='\cdot')  # 使用\cdot代替*
                # 6. 恢复变量格式（确保无空格）
                rhs_latex = re.sub(r'([xy])_\{?(\d+)\}?[ _]\{?(\d+)\}?', r'\1_{\2\3}', rhs_latex)
                # 7. 处理右侧系数（1.0*x → x）
                rhs_latex = re.sub(r'1\.0\s*\\cdot\s*', '', rhs_latex)
                rhs_latex = re.sub(r'1\s*\\cdot\s*', '', rhs_latex)
        except Exception as e:
            raise ValueError(f"无法解析右侧表达式: {rhs} ({str(e)})")

        return f"{lhs_latex} = {rhs_latex}"

    # 处理多表达式情况
    expressions = [e.strip() for e in expression.split(", ") if e.strip()]
    if len(expressions) > 1:
        processed = []
        for expr in expressions:
            if "=" not in expr:
                continue
            processed.append(process_single_expression(expr, False))
        return ", ".join(processed)
    else:
        return process_single_expression(expression, False)
if __name__ == "__main__":
    test_cases = [
        "1.0*y2,1 = 1.0*y1,1",
        "2.5*x3,2 = sin(pi) + 1.0*x1,1",
        "x1,1 = 2.0*x2,1 + 3.5*x3,1"
    ]

    for expr in test_cases:
        print(f"原始表达式: {expr}")
        print(f"LaTeX格式: {convert_to_latex(expr)}\n")