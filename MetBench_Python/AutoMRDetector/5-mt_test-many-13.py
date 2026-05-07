import csv
import datetime
import os
import random
import re
import unittest
import numpy as np
import sympy
from sympy import sympify, Symbol
import get_function
# from JavaInterpreter import *
import os
import glob

def get_absolute_path(relative_path):
    """Convert relative path to absolute path based on the script's location"""
    script_dir = os.path.dirname(os.path.abspath(__file__))
    return os.path.join(script_dir, relative_path)
# 处理多位小数
# 测试类
class RedundancyRemovalTestCase(unittest.TestCase):
    # 配置参数
    @classmethod
    def setUpClass(cls):
        # cls.random_data_count = 1000  # 随机数据数量
        cls.random_data_count = 10  # 随机数据数量
        cls.random_data_min_value = 1  # 随机数据最小值
        cls.random_data_max_value = 10  # 随机数据最大值
        cls.previous_file_path = None
        cls.random_data = None
        # cls.threshold = 1e-4
        # 实验修改
        cls.threshold = 1e-4
        cls.true_count = 0
        cls.false_count = 0

        # cls.threshold_value = 95  # 支持率比例
        cls.threshold_value = 95  # 支持率比例

        # 在这里设置你的 public_path
        # cls.public_path = "output/Redundancy_Removal/"
        # 使用绝对路径
        cls.public_path = get_absolute_path("output/Redundancy_Removal/")
        cls.output_file_directory = get_absolute_path("demo/MR")  # 筛选后文件目录路径

        cls.flat = 0
        cls.successed_output_file = ''
        cls.failed_output_file = ''
        cls.successed_file_type = "successed_mr"  # 成功的文件名
        cls.failed_file_type = "failed_mr"  # 失败的文件名
        cls.successed_title = ["'MethodUnderTest'", "'r'", "'R'", "'MR_dimension'", "'duplicate_count'",
                               "'true_ratio'"]  # 成功的文件标题
        cls.failed_title = ["'MethodUnderTest'", "'r'", "'R'", "'MR_dimension'", "'duplicate_count'",
                            "'true_ratio'"]  # 失败的文件标题
        # cls.output_file_directory = "demo/MR"  # 筛选后文件目录路径

    # 处理CSV文件的测试方法
    def test_process_csv_files(self):
        file_names = os.listdir(self.public_path)
        for file_name in file_names:
            if file_name.endswith(".csv"):
                file_path = os.path.join(self.public_path, file_name)
                # print("file_path:", file_path)
                self.process_csv(file_path)
                self.flat = 0

    # 处理每个CSV文件
    def process_csv(self, csv_path):
        """处理单个CSV文件"""
        with open(csv_path, "r", newline="") as csv_file: # 安全打开文件
            csv_reader = csv.reader(csv_file) # 创建CSV阅读器
            next(csv_reader)  # 跳过标题行
            # 处理行数据时判断是否需要生成新的随机数据
            self.previous_file_path = None # 重置前文件路径记录
            for row in csv_reader:

                if self.flat == 0:  # 如果是文件的第一行数据
                    # 从当前行中获取对应的值
                    func, r, R, MR_dimension, duplicate_count = self.get_row_values(row)

                    # 生成结果文件路径
                    # 测试通过/失败后CSV文件名
                    self.successed_output_file = self.generate_file_path(func, self.random_data_count,
                                                                         self.successed_file_type)
                    self.failed_output_file = self.generate_file_path(func, self.random_data_count,
                                                                      self.failed_file_type)

                    # 创建空的结果文件（带标题行）
                    # 创建空的成功/失败结果文件，并写入列标题
                    self.create_empty_csv(self.successed_output_file, self.successed_title)
                    self.create_empty_csv(self.failed_output_file, self.failed_title)
                    self.flat = self.flat + 1  # 更新处理标志

                # 处理当前行数据
                self.process_row(row, csv_path, self.successed_output_file, self.failed_output_file)

    # 生成文件路径和标题
    def generate_file_path(self, func, num_values, file_type):
        current_time = datetime.datetime.now().strftime("%H时%M分%S秒")
        filename = f"{func}_{num_values}_{current_time}_{file_type}.csv"
        return f"./output/MR/{func}/{file_type}/{filename}"

    # 创建空的CSV文件，并写入列标题
    def create_empty_csv(self, file_path, column_headers):
        os.makedirs(os.path.dirname(file_path), exist_ok=True)
        with open(file_path, 'w', newline='') as csvfile:
            writer = csv.writer(csvfile)
            writer.writerow(column_headers)

    # 格式化小数点后的数字
    def format_numbers(self, data):
        def replace_number(match):
            number = match.group()
            float_number = float(number)
            if float_number == int(float_number):
                # return str(int(float_number))
                return str(float_number)
                # return "{:.1f}".format(float_number)
            else:
                # decimal_part = number.split('.')[-1]
                # if len(decimal_part) > 4:
                #     return "{:.4f}…".format(float_number)
                # else:
                return number

        # 使用正则表达式匹配并替换数字
        format_number_result = re.sub(r'-?\d+\.\d+', replace_number, data)
        return format_number_result

    # 处理CSV文件中的每一行
    def process_row(self, row, file_path, successed_output_file, failed_output_file):
        func, r, R, MR_dimension, duplicate_count = self.get_row_values(row)
        # print("MR:", f"function: {func};", f"r: {r};", f"R: {R}")

        if self.random_data is None or file_path != self.previous_file_path:
            self.random_data = self.generate_random_data(MR_dimension)
            # print("Random Data:", self.random_data)
            # print("\n")
        self.previous_file_path = file_path

        for data_group in self.random_data:
            data_group_str = ', '.join(str(value) for value in data_group)
            x_group_str = f"x1=({data_group_str})"
            variables = self.assign_variables(MR_dimension, data_group)
            func = re.sub(r"\(.*\)", "", func)
            result = self.process_r_expressions(func, r, MR_dimension, variables, x_group_str, R)
            if result:
                self.true_count += 1
            else:
                self.false_count += 1

        # print("true_count:", self.true_count)
        # print("false_count:", self.false_count)
        true_ratio = (self.true_count / len(self.random_data)) * 100
        false_ratio = (self.false_count / len(self.random_data)) * 100
        formatted_true_ratio = "{:.2f}%".format(true_ratio)
        formatted_false_ratio = "{:.2f}%".format(false_ratio)
        # print(f"通过比率: {formatted_true_ratio}")
        # print(f"失败比率: {formatted_false_ratio}")
        self.true_count = 0
        self.false_count = 0

        # 对第二个和第三个数据进行处理
        row[1] = self.format_numbers(row[1])
        row[2] = self.format_numbers(row[2])

        # print("row:", row)

        data_row_with_ratio = row + [formatted_true_ratio]
        if true_ratio >= self.threshold_value:
            self.write_result_to_csv(successed_output_file, data_row_with_ratio)
        else:
            self.write_result_to_csv(failed_output_file, data_row_with_ratio)

        # 对成功和失败的结果文件进行排序
        self.read_and_sort_csv_file(successed_output_file)
        self.read_and_sort_csv_file(failed_output_file)

        # print("=============================================")
        # print("\n")

    # 从CSV中获取行中的值
    def get_row_values(self, row):
        func = row[0]
        r = row[1]
        R = row[2]
        MR_dimension = int(row[3])
        duplicate_count = row[4]
        return func, r, R, MR_dimension, duplicate_count

    # 生成随机数据
    def generate_random_data(self, MR_dimension):

        data = []
        for _ in range(self.random_data_count):
            row = [random.uniform(self.random_data_min_value, self.random_data_max_value) for _ in range(MR_dimension)]
            data.append(row)
        return data

    # 生成随机变量，并为每组数据进行赋值
    def assign_variables(self, MR_dimension, data_group):
        variable_names = [f"x{group + 1},{index + 1}" for group in range(MR_dimension) for index in
                          range(len(data_group))]
        variables = dict(zip(variable_names, data_group))
        original_x = []
        for variable, value in variables.items():
            original_x.append(f"{variable} = {value}")
        # print("original_x:", original_x)
        return variables

    # 获取x的索引
    def get_x_index(self, x_str):
        return int(re.search(r'\d+', x_str).group())

    # 计算 Y 表达式的值
    def calculate_y_values(self, output_y, R, number):
        expression = R.split("=")[number].strip()
        variables = [f"y{i},1" for i in range(1, len(output_y) + 1)]

        for i, y_variable in enumerate(variables, start=1):
            if y_variable in expression:
                y_value_str = output_y[i - 1].split("=")[1].strip()
                try:
                    y_value = float(y_value_str)
                except ValueError:
                    y_value_list = eval(y_value_str)
                    if isinstance(y_value_list, list):
                        y_value = float(y_value_list[0])
                    else:
                        raise ValueError("值既不是浮点数也不是列表")
                expression = expression.replace(y_variable, str(y_value))

        Y_values = eval(expression)
        return Y_values

    # 处理 r 表达式并计算值
    def process_r_expressions(self, func, r, MR_dimension, variables, x_group_str, R):
        x_result = []
        r_values = r.split(", ")
        r_expressions = [r_value.split("=")[1].strip() for r_value in r_values if len(r_value.split("=")) > 1]

        for index, expression in enumerate(r_expressions):
            left_variable = r_values[index].split("=")[0]
            expression = re.sub(r"\b(" + "|".join(variables.keys()) + r")\b", lambda x: f"Symbol('{x.group(0)}')",
                                expression)
            expression = sympify(expression)
            calculate_expression = expression.subs(variables)
            variables[left_variable] = calculate_expression
            mate_calculate_expression = f"{left_variable}:", calculate_expression
            x_result.append(mate_calculate_expression)
        # print("x_result:", x_result)

        x_variables = set()
        for item in x_result:
            variable = item[0].split(",")[0]
            x_variables.add(variable)

        grouped_x_values = []
        grouped_x_values.append(x_group_str)
        for x_variable in sorted(x_variables, key=self.get_x_index):
            x_values = [item[1] for item in x_result if item[0].startswith(x_variable)]
            x_values_str = ', '.join(str(value) for value in x_values)
            grouped_x_values.append(f"{x_variable}=({x_values_str})")

        # print("Grouped x values:", grouped_x_values)

        result = self.calculate_function(grouped_x_values, func, R, MR_dimension)
        return result

    def get_list(self, type):
        # 指定要遍历的文件夹路径
        # folder_path = 'function/' + type
        folder_path = get_absolute_path(f'function/{type}')
        # 创建一个空列表来存储文件名
        file_names = []

        # 使用glob模块获取指定路径下的所有文件
        files = glob.glob(os.path.join(folder_path, '*'))

        # 遍历文件并将去掉后缀的文件名添加到列表中
        for file in files:
            if os.path.isfile(file):  # 确保是文件而不是文件夹
                base_name = os.path.basename(file)  # 获取文件名
                base_name_no_ext = os.path.splitext(base_name)[0]  # 去掉后缀
                file_names.append(base_name_no_ext)  # 将去掉后缀的文件名添加到列表中
        return file_names

    # 计算内部函数的值
    def calculate_function(self, output_row, func, R, MR_dimension):
        y_results = {}
        for i, data in enumerate(output_row, start=1):
            variable = re.findall(r"x(\d+)", data)
            if variable:
                variable = int(variable[0])
                values = re.findall(r"-?\d+\.\d+", data)
                if values:
                    values = np.array([float(value) for value in values])
                    # print("values:", values)
                    python_name = self.get_list("python")
                    java_file = self.get_list("java")
                    function_result = 0
                    if func in python_name:
                        # 外部python
                        # module_path = "function/python/" + func + ".py"
                        module_path = get_absolute_path(f"function/python/{func}.py")
                        function, para = get_function.generate_function(module_path)
                        value = []
                        for j in range(MR_dimension):
                            value.append(values[j])
                        # print(value)
                        function_result = get_function.calculate_value_from_function(function, value)
                    # java函数的mr验证暂时不用
                    # if func in java_file:
                    #     # 外部java
                    #     # java_file_path = "function/java/" + func + ".java"
                    #     java_file_path = get_absolute_path(f"function/java/{func}.java")
                    #     function_name, MR_dimension = get_java_method_name(java_file_path)
                    #     value = []
                    #     for j in range(MR_dimension):
                    #         value.append(values[j])
                    #     function_result = Run_Java_Code(function_name, MR_dimension, *value)

                    y_variable = f"y{i},1"
                    y_results[y_variable] = function_result

        output_y = [f"{y_variable} = {value}" for y_variable, value in y_results.items()]
        # print("output_y:", output_y)

        y_expected = self.calculate_y_values(output_y, R, 0)
        y_actual = self.calculate_y_values(output_y, R, 1)
        # print(f"y_expected = {y_expected}" " ; " f"y_actual = {y_actual}")
        # print("======================================================")

        result = self.compare_y_values(y_expected, y_actual, self.threshold)
        return result

    # 比较 Y 值之间的差异
    def compare_y_values(self, y_expected, y_actual, threshold):
        diff = abs(y_expected - y_actual)
        if diff < threshold:
            return True
        else:
            return False

    # 写入csv
    def write_result_to_csv(self, file_path, data_row):
        with open(file_path, 'a', newline='') as csvfile:
            writer = csv.writer(csvfile)
            writer.writerow(data_row)

    # 排序CSV文件
    def read_and_sort_csv_file(self, file_path):
        with open(file_path, "r", newline="") as csv_file:
            csv_reader = csv.reader(csv_file)
            header = next(csv_reader)  # 获取标题行
            data_rows = [row for row in csv_reader]

        # 根据'duplicate_count'降序和'true_ratio'降序对数据进行排序
        data_rows.sort(key=lambda x: (int(x[4]), -float(x[5].strip('%'))), reverse=True)

        # 将标题行添加回排序后的数据
        sorted_data = [header] + data_rows

        # 将排序后的数据保存回CSV文件
        with open(file_path, 'w', newline='') as csvfile:
            writer = csv.writer(csvfile)
            writer.writerows(sorted_data)

def main():
    """
    运行冗余移除测试的主函数
    返回:
        unittest.TestResult: 包含测试结果的对象
    """
    # 加载测试套件
    suite = unittest.TestLoader().loadTestsFromTestCase(RedundancyRemovalTestCase)

    # 创建测试运行器（可以自定义输出格式）
    runner = unittest.TextTestRunner(verbosity=2)

    # 运行测试并返回结果
    return runner.run(suite)

def main1():
    """命令行直接运行时的入口函数"""
    unittest.main()
if __name__ == "__main__":
    main1()
