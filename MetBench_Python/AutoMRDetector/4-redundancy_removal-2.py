import csv
import os
import settings2

def get_absolute_path(relative_path):
    """Convert relative path to absolute path based on the script's location"""
    script_dir = os.path.dirname(os.path.abspath(__file__))
    return os.path.join(script_dir, relative_path)

def main(csv_folder="./output/CsvFile",
                                   new_csv_folder="./output/Redundancy_Removal"):
    """
    整合所有功能的单一函数：处理CSV文件冗余移除和排序

    参数:
        csv_folder (str): 原始CSV文件所在文件夹路径
        new_csv_folder (str): 处理后CSV文件保存路径

    返回:
        None
    """
    # Convert to absolute paths
    abs_csv_folder = get_absolute_path(csv_folder)
    abs_new_csv_folder = get_absolute_path(new_csv_folder)

    # 确保输出目录存在
    # os.makedirs(new_csv_folder, exist_ok=True)
    os.makedirs(abs_new_csv_folder, exist_ok=True)

    # 遍历原始CSV文件夹
    # for root, dirs, files in os.walk(csv_folder):
    #     for file in files:
    #         if not file.endswith('.csv'):
    #             continue
    for root, dirs, files in os.walk(abs_csv_folder):
        for file in files:
            if not file.endswith('.csv'):
                continue
            # 构建完整文件路径
            file_path = os.path.join(root, file)

            # 处理单个CSV文件
            rows = []
            duplicates = {}

            # 读取CSV文件并检测重复行
            with open(file_path, 'r') as f:
                reader = csv.reader(f)
                for i, row in enumerate(reader):
                    if i == 0:  # 标题行
                        rows.append(row)
                        continue

                    row_tuple = tuple(row)
                    if row_tuple in duplicates:
                        duplicates[row_tuple] += 1
                        continue

                    duplicates[row_tuple] = 1
                    rows.append(row)

            # 构建新文件路径
            # relative_path = os.path.relpath(file_path, csv_folder)
            # new_filename = os.path.join(new_csv_folder, relative_path)
            relative_path = os.path.relpath(file_path, abs_csv_folder)
            new_filename = os.path.join(abs_new_csv_folder, relative_path)
            os.makedirs(os.path.dirname(new_filename), exist_ok=True)

            # 写入带计数的新文件
            with open(new_filename, 'w', newline='') as f:
                writer = csv.writer(f)

                # 写入标题行（添加计数列）
                writer.writerow(rows[0] + ["'Duplicate Count'"])

                # 写入数据行（带计数）
                for row in rows[1:]:
                    writer.writerow(row + [str(duplicates[tuple(row)])])

            # 读取新文件并排序
            with open(new_filename, 'r') as f:
                reader = csv.reader(f)
                header = next(reader)
                sorted_rows = sorted(reader, key=lambda x: int(x[-1]), reverse=True)

            # 写入排序后的数据
            with open(new_filename, 'w', newline='') as f:
                writer = csv.writer(f)
                writer.writerow(header)
                writer.writerows(sorted_rows)

            # print(f"处理完成: {file_path} → {new_filename}")


# 使用示例
if __name__ == "__main__":
    settings2.process_frontend_inputs()
    main()