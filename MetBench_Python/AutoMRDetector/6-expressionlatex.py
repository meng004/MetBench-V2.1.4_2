import re
import sys

from sympy import latex, sympify, evaluate
from sympy.parsing.sympy_parser import parse_expr
import glob
import os
import pandas as pd
import csv
import json

def get_absolute_path(relative_path):
    """Convert relative path to absolute path based on the script's location"""
    script_dir = os.path.dirname(os.path.abspath(__file__))
    return os.path.join(script_dir, relative_path)

CONFIG_FILE = get_absolute_path("config.json")
# 将相对路径转换成绝对路径

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

def process_csv_file(file_path):
    """
    专门处理r和R列的CSV处理器
    功能：
    1. 只修改r和R列的内容（其他列保持不变）
    2. 自动处理列名引号问题
    3. 保留原始文件结构
    """
    # 路径处理
    if not os.path.isabs(file_path):
        file_path = get_absolute_path(file_path)

    if not os.path.exists(file_path):
        raise FileNotFoundError(f"文件不存在: {file_path}")

    # 生成新文件名
    base, ext = os.path.splitext(file_path)
    new_file_path = f"{base}_latex{ext}"

    # 读取CSV（处理可能的列名引号）
    df = pd.read_csv(file_path)
    original_columns = df.columns.tolist()
    # print(f"原始列名: {original_columns}")

    # 创建列名映射字典（原始列名 -> 临时标准化列名）
    column_mapping = {}
    temp_columns = []
    for col in df.columns:
        # 标准化临时列名（去除引号和空格）
        temp_col = str(col).strip().strip("'\"")
        column_mapping[temp_col] = col  # 记录原始列名
        temp_columns.append(temp_col)

    # 标准化列名（去除引号和空格）
    # df.columns = df.columns.str.strip().str.strip("'\"")
    # 临时修改列名为标准化版本
    df.columns = temp_columns

    # 检查目标列是否存在（使用标准化名称）
    target_columns = []
    # 检查必须的列是否存在
    # if 'r' not in df.columns or 'R' not in df.columns:
    #     missing = [col for col in ['r', 'R'] if col not in df.columns]
    #     raise ValueError(f"缺少必要列: {missing}")
    for col in ['r', 'R']:
        if col in df.columns:
            target_columns.append(col)
        else:
            print(f"警告: 未找到列 '{col}'，将跳过该列处理")

    # 仅处理r和R列
    processed_count = 0
    for i in range(len(df)):
        # try:
        #     # 处理r列
        #     if pd.notna(df.at[i, 'r']):
        #         df.at[i, 'r'] = convert_to_latex(str(df.at[i, 'r']))
        #         processed_count += 1
        #
        #     # 处理R列
        #     if pd.notna(df.at[i, 'R']):
        #         df.at[i, 'R'] = convert_to_latex(str(df.at[i, 'R']))
        #         processed_count += 1
        #
        # except Exception as e:
        #     print(f"第{i + 1}行处理失败: {str(e)}")
        #     continue
        for col in target_columns:
            try:
                if pd.notna(df.at[i, col]):
                    original_value = str(df.at[i, col])
                    df.at[i, col] = convert_to_latex(original_value)
                    processed_count += 1
            except Exception as e:
                # print(f"第 {i + 1} 行列 '{col}' 处理失败: {str(e)}")
                # print(f"问题数据: {original_value}")
                continue
        # 恢复原始列名
    final_columns = []
    for temp_col in df.columns:
        # 从映射字典获取原始列名
        original_col = column_mapping.get(temp_col, temp_col)
        final_columns.append(original_col)
    df.columns = final_columns
    # print(f"恢复后的列名: {df.columns.tolist()}")

    # 保存文件（保持原始格式）
    # df.to_csv(file_path, index=False, quoting=csv.QUOTE_NONE, escapechar='\\')
    # 保存到新文件
    df.to_csv(
        new_file_path,
        index=False,
        quoting=csv.QUOTE_NONE,
        escapechar='\\'
    )
    print(f"成功处理 {processed_count} 个表达式（r和R列）")
    print(f"已生成新文件: {new_file_path}")
    return file_path

def find_latest_function_csv(function_name):
    """
    查找指定函数名称对应的最新成功MR CSV文件

    Args:
        function_name (str): 函数名称，如"sin"

    Returns:
        str: 最新CSV文件的绝对路径，如果没有找到则返回None
    """
    # 获取脚本所在目录的绝对路径
    script_dir = os.path.dirname(os.path.abspath(__file__))

    # 构建目标路径模式
    target_pattern = os.path.join(
        script_dir,
        "output",
        "MR",
        f"{function_name}(x)",
        "successed_mr",
        f"{function_name}(x)_*_successed_mr.csv"
    )

    # 查找所有匹配的文件
    matched_files = glob.glob(target_pattern)

    if not matched_files:
        return None

    # 按修改时间排序，获取最新的文件
    latest_file = max(matched_files, key=lambda x: os.path.getmtime(x))

    return latest_file

def load_config():
    """加载配置文件（使用绝对路径）"""
    if not os.path.exists(CONFIG_FILE):
        with open(CONFIG_FILE, 'w') as f:
            json.dump({
                "input_program_filename": None,
                "input_param_count": 1,
                "frontend_input_ranges": None,
                "frontend_input_datatypes": None
            }, f)

    with open(CONFIG_FILE, 'r') as f:
        return json.load(f)

def main():
    # 配置文件使用绝对路径
    config = load_config()
    filename = config["input_program_filename"]
    function_name = ''
    if filename and isinstance(filename, str):
        if filename.endswith('.py'):
            function_name = filename[:-3]
        else:
            function_name = filename
    latest_csv = find_latest_function_csv(function_name)
    if latest_csv:
        print(f"找到最新的CSV文件: {latest_csv}")
        process_csv_file(latest_csv)
    else:
        print(f"没有找到{function_name}(x)对应的CSV文件")
def toLatex(file_path):
    # 路径处理
    if not os.path.isabs(file_path):
        file_path = get_absolute_path(file_path)

    if not os.path.exists(file_path):
        raise FileNotFoundError(f"文件不存在: {file_path}")
    process_csv_file(file_path)
# 测试用例
if __name__ == "__main__":
    csv_path = sys.argv[1]
    toLatex(csv_path)
    # main()
    # test_cases = [
    #     "x1,1 = sin(π) + x2,1",  # 单表达式
    #     "x1,1 = sin(π) + x2,1, x1,2 = x2,2",  # 多表达式
    #     "x2,1 = x1,1 + 2*x1,2",  # 复杂表达式
    #     "y3,2 = π/2",  # 含y变量
    #     "x2,1=-x1,1 - 8.98799969585037"
    # ]
    #
    # for expr in test_cases:
    #     print(f"输入表达式: {expr}")
    #     try:
    #         input_latex= convert_to_latex(expr)
    #         print(f"输入模式: {input_latex}")
    #
    #     except Exception as e:
    #         print(f"错误: {e}\n")
    # function_name = "sin"
    # latest_csv = find_latest_function_csv(function_name)
    # if latest_csv:
    #     print(f"找到最新的CSV文件: {latest_csv}")
    # else:
    #     print(f"没有找到{function_name}(x)对应的CSV文件")
    # process_csv_file(latest_csv)