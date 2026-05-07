import time

import settings2
from load_resulrs import *
import os
import settings
import json
import shutil

# 转换成绝对路径
def get_absolute_path(relative_path):
    """Convert relative path to absolute path based on the script's location"""
    script_dir = os.path.dirname(os.path.abspath(__file__))
    return os.path.join(script_dir, relative_path)
# 每个新的文件夹json重新生成，以及重复执行把之前的删除
def column_to_latex(column, s: str):
    """
    将列名转化为latex形式
    :param column: 待转化的列名
    :param s: 变量名
    :return: 转化好的latex表示
    """
    items = []
    for c in column:
        if type(c) is tuple:
            pass
        item = ''
        if c != '1':  # 如果不是常数项,则将该列名转化为latex公式的形式
            xs = c[5:-1].replace(' ', '').split(',')     # 得到所有的一次项
            es = {}

            # 统计每个一次项的次数
            for x in xs:
                if len(x) == 0:
                    continue
                if x not in es.keys():
                    es[x] = 0
                es[x] += 1

            # 将数据转化为latex公式的形式
            for x in es:
                i, j = x.replace(' ', '')[1:].split('_')
                val = s + '_{' + i + ',' + j + '}'
                if es[x] == 1:
                    item = item + val
                else:
                    item = item + val + '^{}'.format(es[x])
        items.append(item)

    return items


def mat_to_polynomial(mat, item):
    pols = []
    for i in mat:
        pol = []
        for a, x in zip(i, item):
            a = round(a, 3)
            if abs(round(a) - a) < 0.001:
                a = round(a)
            if abs(a) <= 0.001:  # 如果系数是0，直接跳过该项
                continue
            elif len(x) == 0:   # 如果是常数项，直接加入系数
                # pass
                pol.append(str(a))
            elif abs(a - 1.0) <= 0.001: # 如果系数是1，省略系数
                pol.append(x)
            elif abs(a + 1.0) <= 0.001:
                pol.append('-' + x)
            else:   # 否则写成系数和项目相乘的方式
                pol.append(str(a)+x)
        pol = '+'.join(pol)
        pol = pol.replace('+-', '-')
        if len(pol) == 0:
            pol = '0'
        pols.append(pol)
    return pols


def json_to_md(json_path, md_path):

    # json_path = './output/JsonFile/' + global_name_npz_only +'/'+ json_path
    json_path = get_absolute_path(f'./output/JsonFile/{global_name_npz_only}/{json_path}')
    with open(json_path, 'r') as f:
        # print("json_path:",json_path)
        MRs1 = json.load(f)
        MRs = {}
        for i in MRs1:
            MRs[int(i)] = MRs1[i]

        # 指定要保存文件的文件夹路径
        # folder_path = './output/MdFile/' + global_name_npz_only
        folder_path = get_absolute_path(f'./output/MdFile/{global_name_npz_only}')
        # 判断是否需要处理文件夹
        if global_name_npz_only != getattr(json_to_md, 'prev_global_name_npz_only', None):
            # 如果文件夹已存在，则删除文件夹及其内容
            if os.path.exists(folder_path):
                shutil.rmtree(folder_path)

            # 创建文件夹
            os.makedirs(folder_path)

            # 更新 prev_global_name_npz_only
            setattr(json_to_md, 'prev_global_name_npz_only', global_name_npz_only)

        # 保存文件到指定文件夹
        md_save_path = os.path.join(folder_path, md_path)

    with open(md_save_path, 'a', encoding='utf-8') as fw:
        # 写注释：
        # fw.write('> mod:i-j-k表示，共有i次输入，输入模式最高阶数为j, 输出模式最高阶数为k\n')
        for id in sorted(MRs.keys()):
            if len(MRs[id]) == 0:
                continue

            txts = ['## {}. {}'.format(id, MRs[id][0]['name'])]  # 二级标题：函数id加函数名

            con_row = '{} & {} & {}\\\\\n'

            for i, MR in enumerate(MRs[id]):
                # 获得该MR的模式：
                ni = str(MR['num_involved_inputs'])  # 输入元数
                ideg = str(str(MR['input_degrees']))  # 输入关系的阶数
                odeg = str(MR['output_degrees'])  # 输出关系的阶数
                mod = '-'.join([ni, ideg, odeg])

                txts.append('### mod:{}'.format(mod))  # 以模式为三级标题

                # 1. 得到输入关系：
                pols_x1 = mat_to_polynomial(MR['IR'], MR['item_X'])  # 将矩阵转化为多项式
                # 将多项式按输入分组
                step = len(MR['item_X']) - 1
                if step < 1:
                    step = 1
                pols_x2 = [pols_x1[i:i + step] for i in range(0, len(pols_x1), step)]

                # 生成input-relation
                txts.append('**relation of input:**')  # 输入关系
                for i, pol in enumerate(pols_x2):
                    if step == 1:
                        txts.append('$X_{' + str(i+1) + ',1}=' + ','.join(pol) + '$')
                    else:
                        irl = '({})'.format(','.join(['X_{' + str(i) + ',' + str(j+1) + '}' for j in range(step)]))
                        irr = '({})'.format(','.join(pol))
                        txts.append('${}={}$'.format(irl, irr))

                # 生成输入关系表达式
                txts.append('\n**output:**')  # 输入关系
                pols_x = {'Y_{0,1}': '{}({})'.format(MR['name'], ','.join(MR['item_X'][1:]))}  # 提前在项中放入X_0
                for i, pol in enumerate(pols_x2):
                    pols_x['Y_{' + str(i + 1) + ',1}'] = '{}({})'.format(MR['name'], ','.join(pol))  # 生成Y_{i,j}的表达式

                # 得到输入关系：
                # txts.append('**IR:**')
                for p in pols_x:
                    txts.append('${}={}$'.format(p, pols_x[p]))
                txts.append('')

                # 2. 得到MR关系：
                pols_y = mat_to_polynomial(MR['OR'], MR['item_Y'])
                mrs = ';'.join(pols_y)
                mrs = mrs.split(';')
                for i, mr in enumerate(mrs):
                    txts.append('**MR{}:** ${}=0$;'.format(i + 1, mr))

            txts = [txt + '\n' for txt in txts]
            txts.append('\n')
            fw.writelines(txts)

global_name_npz = ''  # 全局变量
global_fun_name = ''
global_name_npz_only = ''
def npz_to_json(npz_root, json_path):
    global global_name_npz  # 声明要使用的全局变量
    global global_fun_name
    global global_name_npz_only

    # path = npz_root     # 将path指向需要读取的文件夹
    path = get_absolute_path(npz_root)  # 将path指向需要读取的文件夹

    # 读取文件夹中的文件
    npz_list = os.listdir(path)
    map_MR = {}  # 存放蜕变关系字典，key为序号

    # 依次处理每一个npz文件
    for name_npz in npz_list:
        if name_npz.split('.')[-1] == 'npz':
            par = name_npz.split('_')[0:6]  # 获得参数
            par = list(map(int, par))  # 将参数转为 int
            if par[0] not in map_MR.keys():  # 第一次遇到id为par[0]的函数，将map_MR[par[0]]初始化为一个列表
                map_MR[par[0]] = []

            # 获得par[0]的函数名字，如果找不到名字，则暂时命名为 str(par[0])
            # if par[0] in settings.map_index_func.keys():
            map_index_func = settings2.get_function_map()
            if par[0] in map_index_func.keys():
            # if par[0] in settings1.map_index_func.keys():
                # fun_name = settings.map_index_func[par[0]]
                # fun_name = settings1.map_index_func[par[0]]
                fun_name = map_index_func[par[0]]
            else:
                fun_name = str(par[0])

            global_fun_name = fun_name

            # print(str(fun_name) + '_' + name_npz + '-------')
            t_dict = {  # 临时字典
                'name': fun_name,  # 函数名
                'num_involved_inputs': par[1],  # 当前关系的元数
                'is_equal': (par[2] == 1 and par[3] == 1),  # 是否为对等关系
                'input_degrees': par[4],  # 输入模式的阶数
                'output_degrees': par[5]  # 输出模式的阶数
            }

            if t_dict['is_equal']:  # 暂时只考虑对等关系
                # if True:
                MRs = load_phase3_results(path + name_npz)  # 读取MR关系
                if MRs is None:
                    return None
                for k, v in MRs.items():
                    # 处理列名：
                    # print(len(MRs.items()))
                    latex_x = column_to_latex(v[0].columns, 'X')
                    latex_y = column_to_latex(v[1].columns, 'Y')
                    # print(latex_x)
                    # print(latex_y)

                    # 保存关系
                    t_dict['item_X'] = latex_x
                    t_dict['item_Y'] = latex_y
                    t_dict['IR'] = v[0].values.tolist()
                    t_dict['OR'] = v[1].values.tolist()
                map_MR[par[0]].append(t_dict)

                # 在满足条件时进行赋值
                global_name_npz = name_npz  # 将 name_npz 赋值给全局变量
                # print("djslkajsjal:", global_name_npz)
                if '_after' in global_name_npz:
                    global_name_npz_only = global_name_npz.split('_after', 1)[0]
                else:
                    global_name_npz_only = global_name_npz
                # print("global_name_npz_only:::",global_name_npz_only)

    # 指定要保存文件的文件夹路径
    # folder_path = './output/JsonFile/' + global_name_npz_only
    # 指定要保存文件的文件夹路径
    folder_path = get_absolute_path(f'./output/JsonFile/{global_name_npz_only}')
    # 判断是否需要处理文件夹
    if global_name_npz_only != getattr(npz_to_json, 'prev_global_name_npz_only', None):
        # 如果文件夹已存在，则删除文件夹及其内容
        if os.path.exists(folder_path):
            shutil.rmtree(folder_path)

        # 创建文件夹
        os.makedirs(folder_path)

        # 更新 prev_global_name_npz_only
        setattr(npz_to_json, 'prev_global_name_npz_only', global_name_npz_only)

    # 保存文件到指定文件夹
    # 更新json_path以便传递给j to md方法
    json_save_path = os.path.join(folder_path, json_path)

    with open(json_save_path, "w") as f:
        f.write(json.dumps(map_MR, ensure_ascii=False, indent=4, separators=(',', ':')))
    return 1

def custom_sort(item):
    try:
        return int(item)
    except ValueError:
        return item


def main():
    """封装原来的主代码块为一个函数"""
    # folder_path = 'output/Individual_Laboratory/'
    folder_path = get_absolute_path('output/Individual_Laboratory/')
    # 收集所有 phase3 文件夹的路径
    phase3_paths = []
    for root, dirs, files in os.walk(folder_path):
        dirs.sort(key=custom_sort)  # 自定义排序函数

        for dir_name in dirs:
            if dir_name == 'phase3':
                phase3_paths.append(os.path.join(root, dir_name).replace('\\', '/'))

    prev_data = None
    index = 1
    # 遍历 phase3 文件夹路径并进行处理
    for phase3_path in phase3_paths:
        parts = phase3_path.split('/')
        data = parts[-4]

        if data != prev_data:
            index = 1
            prev_data = data
        else:
            index += 1

        json_name = '{}_json'.format(index) + time.strftime("_%Hh_%Mm_%Ss", time.localtime())
        markdown_name = '{}_npmd'.format(index) + time.strftime("_%Hh_%Mm_%Ss", time.localtime())
        npz_root = phase3_path + '/'  # npz文件目录，请最后一定要以/结尾。

        # 生成单独的JSON文件路径
        npz_to_json(npz_root, '{}.json'.format(json_name))  # 将npz文件转换为json文件
        json_to_md('{}.json'.format(json_name), '{}.md'.format(markdown_name))  # 将json文件转化为md文件


if __name__ == '__main__':
    # settings1.process_frontend_inputs()
    # print("npz转md实验开始---")
    main()