import subprocess
import sys
import time
from queue import Queue
import settings2
import json
import os
from importlib import import_module


# 获取基础目录路径
BASE_DIR = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, BASE_DIR)  # 确保可以导入同目录模块
# 常量定义

COMPLETE_FLAG = os.path.join(BASE_DIR, "script_complete.flag")  # 完成标记文件路径
def get_abs_path(relative_path):
    """将相对路径转换为绝对路径"""
    return os.path.join(BASE_DIR, relative_path)

def clean_complete_flag():
    """清理完成标记文件"""
    if os.path.exists(COMPLETE_FLAG):
        os.remove(COMPLETE_FLAG)
# 配置文件使用绝对路径
CONFIG_FILE = get_abs_path("config.json")

def wait_for_completion(timeout=None):
    """
    等待完成标记文件出现
    :param timeout: 超时时间（秒），None表示无限等待
    :return: True检测到标记，False超时
    """
    start_time = time.time()
    while True:
        if os.path.exists(COMPLETE_FLAG):
            clean_complete_flag()
            return True

        if timeout and (time.time() - start_time > timeout):
            return False

        time.sleep(300)  # 每5秒检查一次
def run_script(script_path):
    """执行脚本（使用绝对路径）"""
    abs_script_path = get_abs_path(script_path)
    script_name = os.path.splitext(os.path.basename(script_path))[0]

    print(f"\n开始执行 {abs_script_path}...")
    start_time = time.time()

    try:
        if script_path == "5-mt_test-many-13.py":
            result = subprocess.run(
                [sys.executable, abs_script_path],  # 使用绝对路径
                cwd=BASE_DIR,  # 设置工作目录
                check=True
            )
        else:
            module = import_module(script_name)
            if hasattr(module, 'main'):
                module.main()

        elapsed = time.time() - start_time
        print(f"{abs_script_path} 执行完成 (耗时: {elapsed:.2f}s)")
    except Exception as e:
        elapsed = time.time() - start_time
        print(f"脚本 {abs_script_path} 执行失败 (耗时: {elapsed:.2f}s): {e}")


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


def save_config(config):
    """保存配置文件（使用绝对路径）"""
    with open(CONFIG_FILE, 'w') as f:
        json.dump(config, f, indent=4)


def main():
    # 确保输出目录存在
    output_dir = get_abs_path("output")
    os.makedirs(output_dir, exist_ok=True)
    # 从前端获取到的参数
    args = sys.argv[1:]  # 跳过脚本名
    # 1. 首先处理前端输入
    print("正在处理前端输入...")
    filename = args[0]
    # # 这个要转换成int型
    param_count = int(args[1])
    range_str = args[2]
    datatype_str = args[3]
    # 将从前端读到的数据写入到
    config = load_config()
    config["input_program_filename"] = filename
    config["input_param_count"] = param_count
    config["frontend_input_ranges"] = range_str
    config["frontend_input_datatypes"] = datatype_str
    save_config(config)

    # 定义要执行的脚本（相对路径）
    scripts = [
        # "1_individual_laboratory-10.py",
        "2_npz2md-1.py",
        "3_npcsv_many-15.py",
        "4-redundancy_removal-2.py",
        "5-mt_test-many-13.py"
    ]
    # 执行脚本
    total_start = time.time()
    for script in scripts:
        run_script(script)
    print(f"\n所有脚本执行完成 (总耗时: {time.time() - total_start:.2f}s)")
    print("\n所有脚本执行完成")
    sys.exit(0)


if __name__ == "__main__":
    main()
    sys.exit(0)
