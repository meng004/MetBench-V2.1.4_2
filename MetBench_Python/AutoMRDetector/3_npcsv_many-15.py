import json
import os
import re
import csv

from sympy import Symbol, simplify
from datetime import datetime

BASE_DIR = os.path.dirname(os.path.abspath(__file__))
# 将相对路径转换成绝对路径
def get_absolute_path(relative_path):
    """Convert relative path to absolute path based on the script's location"""
    script_dir = os.path.dirname(os.path.abspath(__file__))
    return os.path.join(script_dir, relative_path)

# 处理较小数据

threshold = 1e-6

# 根据当前时间生成文件名
current_time = datetime.now().strftime("%H时%M分%S秒")
# 如果transform_csv文件夹不存在，则创建
# os.makedirs('./output/CsvFile', exist_ok=True)
os.makedirs(get_absolute_path('./output/CsvFile'), exist_ok=True)
# 获取JSON文件夹路径
# json_folder = './output/JsonFile/'
json_folder = get_absolute_path('./output/JsonFile/')
final_output = []

# 递归函数，用于获取文件夹下所有JSON文件的路径和键值
def get_json_file_data(folder):
    file_data = []
    for root, dirs, files in os.walk(folder):
        for file in files:
            if file.endswith('.json'):
                json_path = os.path.join(root, file)
                with open(json_path, 'r') as file:
                    data = json.load(file)
                for key in data:
                    if key.isdigit():
                        file_data.append((json_path, int(key), data[key]))
    return file_data


# 定义排序函数
def extract_number(filename):
    match = re.search(r'(\d+)_json', filename)
    if match:
        return int(match.group(1))
    return 0


# IR使用的函数
# 定义组合函数
def combine_expressions(expressions, dimension):
    combinations_list = []
    for i in range(0, len(expressions), dimension):
        combination = expressions[i:i + dimension]
        combinations_list.append(combination)
        # print("combinations_list", combinations_list)
    return combinations_list


# IR和OR公共使用
# 定义构建表达式的函数
def build_expression(variable, sublist, item):
    terms = []
    for i in range(1, len(sublist)):
        if item[i] != "":
            index = item[i].split('_')[-1].strip('{}')  # 提取索引值
            term = sublist[i] * Symbol(f"{variable}_{index}")  # 构建项
            terms.append(term)


    if abs(sublist[0]) <= threshold:
        sublist[0] = 0.0

    # sublist_0 = round(sublist[0], 6)  # 保留六位小数

    expression = sublist[0] + sum(terms)  # 构建表达式
    simplified_expression = simplify(expression)  # 进行表达式的简化

    return simplified_expression


# OR使用的函数
# 定义获取element_divide的函数
def get_element_divide(sublist, item_Y):
    item_Y_index = len(item_Y) - 1
    zero_count = 0
    for i in range(len(sublist) - 1, -1, -1):
        if sublist[i] == 0.0:
            zero_count += 1
        else:
            break
    if zero_count == 1:
        index = item_Y[item_Y_index - 1].split('_')[-1].strip('{}')
        element_divide = Symbol(f"Y_{index}")
    elif zero_count == 2:
        index = item_Y[item_Y_index - 2].split('_')[-1].strip('{}')
        element_divide = Symbol(f"Y_{index}")
    elif zero_count >= 3:
        index = item_Y[item_Y_index - zero_count].split('_')[-1].strip('{}')
        element_divide = Symbol(f"Y_{index}")
    else:
        index = item_Y[item_Y_index].split('_')[-1].strip('{}')
        element_divide = Symbol(f"Y_{index}")

    return element_divide


# 定义处理R表达式的函数
def process_expression(expression, element_divide):
    left_expression = expression.coeff(element_divide) * element_divide
    right_expression = expression - left_expression

    right_expression = -right_expression

    if left_expression.coeff(element_divide) != 1.0:
        coefficient = left_expression.coeff(element_divide)
        right_expression /= coefficient
        left_expression /= coefficient

    return left_expression, right_expression


# 定义处理IR的函数
def process_ir(item_X, IR, OR, MR_dimension):
    ir_expressions = []
    for sublist in IR:
        # print("sublist",sublist)
        ir_simplified_expression = build_expression('x', sublist, item_X)
        # print("ir_simplified_expression",ir_simplified_expression)

        ir_expressions.append(ir_simplified_expression)
    # print("ir_expressions:",ir_expressions)

    # 进行组合
    combinations_list = combine_expressions(ir_expressions, MR_dimension)

    return combinations_list


# 定义处理OR的函数
def process_or(item_Y, OR):
    total_expression_append = []
    for sublist in OR:
        or_simplified_expression = build_expression('Y', sublist, item_Y)

        element_divide = get_element_divide(sublist, item_Y)

        left_expression, right_expression = process_expression(or_simplified_expression, element_divide)

        total_expression = f"{left_expression} = {right_expression}"
        total_expression_append.append(total_expression)
    # print("total_expression_append:", total_expression_append)

    return total_expression_append


# 下划线后面数字+1
def modify_expression_with_indices(expressions):

    matches = re.findall(r'_(\d+)', expressions)
    indices_expressions = [int(match) for match in matches]
    has_zero_expressions = 0 in indices_expressions

    new_indices_expressions = [
        index + 1 if has_zero_expressions and expressions.find(f"_{index}") != -1 else index for index in
        indices_expressions]

    # 构造替换字典
    replace_dict = {}
    for i, index in enumerate(indices_expressions):
        if has_zero_expressions and expressions.find(f"_{index}") != -1:
            replace_dict[f"_{index}"] = f"_{new_indices_expressions[i]}"

    def replace_func(match):
        matched_str = match.group()
        return replace_dict.get(matched_str, matched_str)

    # 使用匿名函数替换原始表达式中的数字
    expressions = re.sub(r'_\d+', replace_func, expressions)

    return expressions


# 获取JSON文件路径和键值
json_data = get_json_file_data(json_folder)

# 对json_data进行排序
json_data = sorted(json_data, key=lambda x: extract_number(x[0]))


# final_output = []


# 写入CSV文件
def write_to_csv(file_path, data):
    if os.path.isfile(file_path) and os.stat(file_path).st_size != 0:
        # 文件不为空，只写入数据行
        with open(file_path, 'a', newline='') as file:
            writer = csv.writer(file)
            writer.writerow(data[0:])  # 只写入数据行，不包括列标题
    else:
        # 文件为空，写入列标题和数据行
        with open(file_path, 'w', newline='') as file:
            writer = csv.writer(file)
            writer.writerow(["'MethodUnderTest'", "'r'", "'R'", "'MR_dimension'"])  # 列标题
            writer.writerow(data)


def modify_variables(or_expression, item_Y_len):
    # 用于存储在or_expression中存在的Y变量的索引的列表
    y_variable_indices = []

    # 生成一个包含Y变量和对应索引的列表，索引从2开始，一直到num_y_variables + 1
    y_variables = [f'Y_{i},' for i in range(2, item_Y_len + 2)]

    # 检查or_expression中是否存在y_variables中的每个Y变量
    for i, y_variable in enumerate(y_variables):
        if y_variable in or_expression:
            y_variable_indices.append(i)

    # 初始化一个变量来跟踪替换的索引（从2开始）
    replacement_index = 2
    # 将or_expression中的Y变量替换为连续的索引，从Y_2开始
    for i in y_variable_indices:
        or_expression = or_expression.replace(y_variables[i], f'Y_{replacement_index},')
        replacement_index += 1

    return or_expression

# 遍历JSON文件路径和键值
for json_path, key, data in json_data:
    # 替换反斜杠为正斜杠
    json_path = json_path.replace('\\', '/')
    # print("json_path:",json_path)
    # print(f"Processing Key: {key}")

    # 检查"data"是否为空
    if not data:
        continue

    # 清空final_output列表，以处理新的json_path
    final_output = []

    # 获取数据
    result = data

    # 定义一个列表来存储每行的数据
    csv_data = []

    for item in result:
        name = item['name']
        name_with_x = f"{name}(x)"
        num_involved_inputs = item['num_involved_inputs']
        is_equal = item['is_equal']
        input_degrees = item['input_degrees']
        output_degrees = item['output_degrees']
        item_X = item['item_X']
        item_Y = item['item_Y']
        IR = item['IR']
        OR = item['OR']

        # 获取IR和OR的数组个数
        IR_array_count = len(IR)
        OR_array_count = len(OR)

        # 计算MR维数
        MR_dimension = IR_array_count // OR_array_count

        # 输出IR简化后的表达式
        ir_combinations_list = process_ir(item_X, IR, OR, MR_dimension)
        # print("ir_combinations_list:",ir_combinations_list)

        # 输出OR简化后的表达式
        or_total_expression_append = process_or(item_Y, OR)
        # print("or_total_expression_append:",or_total_expression_append)

        for or_expression in or_total_expression_append:
            matches = re.findall(r'_(\d+)', or_expression)
            matches.sort(key=int)  # 对matches进行排序，按照数字大小进行升序排序
            # print("matches",matches)
            indices = [int(match) for match in matches]
            # print("indices",indices)
            expression_list = []
            for index in indices:
                if index <= len(ir_combinations_list) and index > 0:
                    expression_list.append(ir_combinations_list[index-1])
                    # print("expression_list：",expression_list)

            combined_expressions = "#".join(
                "({})".format(", ".join(str(expression) for expression in expressions)) for expressions in
                expression_list)
            # print("combined_expressions：",combined_expressions)
            final_output.append([combined_expressions, or_expression])
        # print("Final Output:",final_output)

        final_output_modified = []  # 用于存储修改后的Final Output

        for data_group in final_output:
            expressions, or_expression = data_group
            # r_values = expressions.split("#")
            # print("expressions",len(r_values))

            # 处理expressions
            expressions = modify_expression_with_indices(expressions)

            # 处理or_expression
            or_expression = modify_expression_with_indices(or_expression)

            final_output_modified.append([expressions, or_expression])

        # print("final_output_modified:",final_output_modified)


        # 定义一个函数根据位置和数量生成变量名
        def generate_variable_names(position, count):
            return ', '.join(f'x{position},{i + 1}' for i in range(count))

        # 处理final_output_modified数据
        processed_final_output = []
        for data_group in final_output_modified:
            expressions, or_expression = data_group
            r_values = expressions.split("#")

            # 为每个独立的表达式生成变量赋值语句
            variable_assignments = []
            for i, expression in enumerate(r_values):
                variable_names = generate_variable_names(i + 2, len(expression.split(", ")))
                expression_with_assignments = f'({variable_names}) = {expression}'
                variable_assignments.append(expression_with_assignments)

            # 将原始表达式中的旧变量替换为新生成的变量
            for i in range(len(variable_assignments)):
                expressions = expressions.replace(r_values[i], variable_assignments[i])

            # 将处理后的数据添加到新的列表中
            processed_final_output.append([expressions, or_expression])
        # print("processed_final_output:",processed_final_output)


        # 处理processed_final_output数据
        processed_final_output_modified = []
        for data_group in processed_final_output:
            expressions, or_expression = data_group
            r_values = expressions.split("#")

            # 处理每个独立的表达式
            variable_assignments = []
            for expression in r_values:
                # 按等号分割表达式左右两部分
                lhs, rhs = expression.split("=")
                # 去除空格，并将等号左边的变量名和等号右边的数值分开
                variables = [var.strip().replace('(', '').replace(')', '') for var in lhs.split(", ")]
                values = [value.strip().replace('(', '').replace(')', '') for value in rhs.split(", ")]
                # 生成变量赋值语句，并将变量名和数值一一对应
                assignments = ', '.join(f'{var}={value}' for var, value in zip(variables, values))
                variable_assignments.append(assignments)

            # 将处理后的数据添加到新的列表中，并使用逗号分隔表达式
            processed_final_output_modified.append([', '.join(variable_assignments), or_expression])
                # print(processed_final_output_modified[0])
        # ("processed_final_output_modified:",processed_final_output_modified)
        # print("-------------------------------------------------------")


        # 设置CSV文件路径
        # csv_file = f"./output/CsvFile/{name}(x)_{num_involved_inputs}_{input_degrees}_{output_degrees}_MR_{current_time}.csv"
        # csv_file = get_absolute_path(
        #     f'./output/CsvFile/{name}(x)_{num_involved_inputs}_{input_degrees}_{output_degrees}_MR_{current_time}.csv'
        # )
        csv_file = os.path.join(BASE_DIR, "output", "CsvFile",
                                f"{name}(x)_{num_involved_inputs}_{input_degrees}_{output_degrees}_MR_{current_time}.csv")
        # print("csv_filecsv_filecsv_file",csv_file)
        # 处理processed_final_output_modified数据
        for data_group in processed_final_output_modified:
            expressions, or_expression = data_group

            or_expression = modify_variables(or_expression, OR_array_count)

            # 移除下划线
            expressions = expressions.replace('_', '')
            or_expression = or_expression.replace('_', '')
            # 替换Y为y
            or_expression = or_expression.replace('Y', 'y')

            row = [name_with_x, expressions, or_expression, MR_dimension]

            # 将数据写入csv文件
            write_to_csv(csv_file,row)


def main():
    """主处理流程"""
    global final_output

    # 获取并排序JSON数据
    json_data = get_json_file_data(json_folder)
    json_data = sorted(json_data, key=lambda x: extract_number(x[0]))

    for json_path, key, data in json_data:
        json_path = json_path.replace('\\', '/')
        # print("Processing:", json_path)

        if not data:
            continue

        final_output = []

        for item in data:
            # 提取数据
            name = item['name']
            name_with_x = f"{name}(x)"
            num_involved_inputs = item['num_involved_inputs']
            input_degrees = item['input_degrees']
            output_degrees = item['output_degrees']
            item_X = item['item_X']
            item_Y = item['item_Y']
            IR = item['IR']
            OR = item['OR']

            # 计算MR维数
            MR_dimension = len(IR) // len(OR)

            # 处理IR和OR
            ir_combinations = process_ir(item_X, IR, OR, MR_dimension)
            or_expressions = process_or(item_Y, OR)

            # 构建最终输出
            for or_expr in or_expressions:
                matches = sorted(re.findall(r'_(\d+)', or_expr), key=int)
                indices = [int(match) for match in matches]
                expr_list = [ir_combinations[index - 1] for index in indices
                             if index <= len(ir_combinations) and index > 0]

                combined = "#".join(f"({', '.join(str(e) for e in exprs)})"
                                    for exprs in expr_list)
                final_output.append([combined, or_expr])

            # 表达式修改
            modified_output = []
            for data_group in final_output:
                expr, or_expr = data_group
                modified_expr = modify_expression_with_indices(expr)
                modified_or = modify_expression_with_indices(or_expr)
                modified_output.append([modified_expr, modified_or])

            # 变量赋值处理
            processed_output = []
            for data_group in modified_output:
                expr, or_expr = data_group
                r_values = expr.split("#")

                assignments = []
                for i, expression in enumerate(r_values):
                    vars = generate_variable_names(i + 2, len(expression.split(", ")))
                    assignments.append(f'({vars}) = {expression}')

                processed_expr = "#".join(assignments)
                processed_output.append([processed_expr, or_expr])

            # 最终格式化
            final_processed = []
            for data_group in processed_output:
                expr, or_expr = data_group
                r_values = expr.split("#")

                var_assignments = []
                for expression in r_values:
                    lhs, rhs = expression.split("=")
                    vars = [v.strip().replace('(', '').replace(')', '') for v in lhs.split(", ")]
                    vals = [v.strip().replace('(', '').replace(')', '') for v in rhs.split(", ")]
                    var_assignments.append(', '.join(f'{var}={val}' for var, val in zip(vars, vals)))
                    # try:
                    #     # 使用 split("=", 1) 确保只分割第一个等号
                    #     lhs, rhs = expression.split("=", 1)
                    #
                    #     # 清理并分割变量和值
                    #     vars = [v.strip().replace('(', '').replace(')', '') for v in lhs.split(",") if v.strip()]
                    #     vals = [v.strip().replace('(', '').replace(')', '') for v in rhs.split(",") if v.strip()]
                    #
                    #     # 确保变量和值数量匹配
                    #     if vars and vals:
                    #         assignments = ', '.join(f'{var}={val}' for var, val in zip(vars, vals))
                    #         var_assignments.append(assignments)
                    #     else:
                    #         print(f"跳过无效表达式（变量或值为空）: {expression}")
                    #
                    # except ValueError as e:
                    #     print(f"跳过无效表达式（分割错误）: {expression} | 错误: {str(e)}")
                    #     continue
                    # except Exception as e:
                    #     print(f"跳过表达式（未知错误）: {expression} | 错误: {str(e)}")
                    #     continue
                final_processed.append([', '.join(var_assignments), or_expr])

            # 写入CSV
            # csv_file = f"./output/CsvFile/{name}(x)_{num_involved_inputs}_{input_degrees}_{output_degrees}_MR_{current_time}.csv"
            # csv_file = f"./output/CsvFile/{name}(x)_{num_involved_inputs}_{input_degrees}_{output_degrees}_MR_{current_time}.csv"
            csv_file = os.path.join(BASE_DIR, "output", "CsvFile",
                                    f"{name}(x)_{num_involved_inputs}_{input_degrees}_{output_degrees}_MR_{current_time}.csv")

            for data_group in final_processed:
                expressions, or_expr = data_group
                or_expr = modify_variables(or_expr, len(OR))
                expressions = expressions.replace('_', '')
                or_expr = or_expr.replace('_', '').replace('Y', 'y')

                row = [name_with_x, expressions, or_expr, MR_dimension]
                write_to_csv(csv_file, row)


if __name__ == '__main__':
    main()