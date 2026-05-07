import time
import settings1
from load_resulrs import *
import os
import settings
import json
import shutil
from pathlib import Path

# 获取基础目录路径
BASE_DIR = Path(__file__).parent.absolute()


def get_abs_path(relative_path):
    """将相对路径转换为绝对路径"""
    return str(BASE_DIR / relative_path)


# 每个新的文件夹json重新生成，以及重复执行把之前的删除
def column_to_latex(column, s: str):
    """（原有代码保持不变）"""
    items = []
    for c in column:
        if type(c) is tuple:
            pass
        item = ''
        if c != '1':
            xs = c[5:-1].replace(' ', '').split(',')
            es = {}
            for x in xs:
                if len(x) == 0:
                    continue
                if x not in es.keys():
                    es[x] = 0
                es[x] += 1
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
    """（原有代码保持不变）"""
    pols = []
    for i in mat:
        pol = []
        for a, x in zip(i, item):
            a = round(a, 3)
            if abs(round(a) - a) < 0.001:
                a = round(a)
            if abs(a) <= 0.001:
                continue
            elif len(x) == 0:
                pol.append(str(a))
            elif abs(a - 1.0) <= 0.001:
                pol.append(x)
            elif abs(a + 1.0) <= 0.001:
                pol.append('-' + x)
            else:
                pol.append(str(a) + x)
        pol = '+'.join(pol)
        pol = pol.replace('+-', '-')
        if len(pol) == 0:
            pol = '0'
        pols.append(pol)
    return pols


def json_to_md(json_path, md_path):
    # 修改为绝对路径
    json_abs_path = get_abs_path(f'./output/JsonFile/{global_name_npz_only}/{json_path}')
    folder_abs_path = get_abs_path(f'./output/MdFile/{global_name_npz_only}')

    with open(json_abs_path, 'r') as f:
        MRs1 = json.load(f)
        MRs = {}
        for i in MRs1:
            MRs[int(i)] = MRs1[i]

        # 判断是否需要处理文件夹
        if global_name_npz_only != getattr(json_to_md, 'prev_global_name_npz_only', None):
            if os.path.exists(folder_abs_path):
                shutil.rmtree(folder_abs_path)
            os.makedirs(folder_abs_path)
            setattr(json_to_md, 'prev_global_name_npz_only', global_name_npz_only)

        md_save_path = os.path.join(folder_abs_path, md_path)

    with open(md_save_path, 'a', encoding='utf-8') as fw:
        # 原有内容保持不变
        for id in sorted(MRs.keys()):
            if len(MRs[id]) == 0:
                continue
            txts = ['## {}. {}'.format(id, MRs[id][0]['name'])]
            con_row = '{} & {} & {}\\\\\n'
            for i, MR in enumerate(MRs[id]):
                ni = str(MR['num_involved_inputs'])
                ideg = str(str(MR['input_degrees']))
                odeg = str(MR['output_degrees'])
                mod = '-'.join([ni, ideg, odeg])
                txts.append('### mod:{}'.format(mod))
                pols_x1 = mat_to_polynomial(MR['IR'], MR['item_X'])
                step = len(MR['item_X']) - 1
                if step < 1:
                    step = 1
                pols_x2 = [pols_x1[i:i + step] for i in range(0, len(pols_x1), step)]
                txts.append('​**​relation of input:​**​')
                for i, pol in enumerate(pols_x2):
                    if step == 1:
                        txts.append('$X_{' + str(i + 1) + ',1}=' + ','.join(pol) + '$')
                    else:
                        irl = '({})'.format(','.join(['X_{' + str(i) + ',' + str(j + 1) + '}' for j in range(step)]))
                        irr = '({})'.format(','.join(pol))
                        txts.append('${}={}$'.format(irl, irr))
                txts.append('\n​**​output:​**​')
                pols_x = {'Y_{0,1}': '{}({})'.format(MR['name'], ','.join(MR['item_X'][1:]))}
                for i, pol in enumerate(pols_x2):
                    pols_x['Y_{' + str(i + 1) + ',1}'] = '{}({})'.format(MR['name'], ','.join(pol))
                for p in pols_x:
                    txts.append('${}={}$'.format(p, pols_x[p]))
                txts.append('')
                pols_y = mat_to_polynomial(MR['OR'], MR['item_Y'])
                mrs = ';'.join(pols_y)
                mrs = mrs.split(';')
                for i, mr in enumerate(mrs):
                    txts.append('​**​MR{}:​**​ ${}=0$;'.format(i + 1, mr))
            txts = [txt + '\n' for txt in txts]
            txts.append('\n')
            fw.writelines(txts)


global_name_npz = ''
global_fun_name = ''
global_name_npz_only = ''


def npz_to_json(npz_root, json_path):
    global global_name_npz
    global global_fun_name
    global global_name_npz_only

    # 修改为绝对路径
    abs_npz_root = get_abs_path(npz_root) if not os.path.isabs(npz_root) else npz_root
    folder_abs_path = get_abs_path(f'./output/JsonFile/{global_name_npz_only}')

    # 读取文件夹中的文件
    npz_list = os.listdir(abs_npz_root)
    map_MR = {}

    for name_npz in npz_list:
        if name_npz.split('.')[-1] == 'npz':
            par = name_npz.split('_')[0:6]
            par = list(map(int, par))
            if par[0] not in map_MR.keys():
                map_MR[par[0]] = []

            map_index_func = settings1.get_function_map()
            if par[0] in map_index_func.keys():
                fun_name = map_index_func[par[0]]
            else:
                fun_name = str(par[0])

            global_fun_name = fun_name
            # print(str(fun_name) + '_' + name_npz + '-------')
            t_dict = {
                'name': fun_name,
                'num_involved_inputs': par[1],
                'is_equal': (par[2] == 1 and par[3] == 1),
                'input_degrees': par[4],
                'output_degrees': par[5]
            }

            if t_dict['is_equal']:
                MRs = load_phase3_results(os.path.join(abs_npz_root, name_npz))
                if MRs is None:
                    return None
                for k, v in MRs.items():
                    latex_x = column_to_latex(v[0].columns, 'X')
                    latex_y = column_to_latex(v[1].columns, 'Y')
                    t_dict['item_X'] = latex_x
                    t_dict['item_Y'] = latex_y
                    t_dict['IR'] = v[0].values.tolist()
                    t_dict['OR'] = v[1].values.tolist()
                map_MR[par[0]].append(t_dict)
                global_name_npz = name_npz
                if '_after' in global_name_npz:
                    global_name_npz_only = global_name_npz.split('_after', 1)[0]
                else:
                    global_name_npz_only = global_name_npz

    # 判断是否需要处理文件夹
    if global_name_npz_only != getattr(npz_to_json, 'prev_global_name_npz_only', None):
        if os.path.exists(folder_abs_path):
            shutil.rmtree(folder_abs_path)
        os.makedirs(folder_abs_path)
        setattr(npz_to_json, 'prev_global_name_npz_only', global_name_npz_only)

    json_save_path = os.path.join(folder_abs_path, json_path)
    with open(json_save_path, "w") as f:
        f.write(json.dumps(map_MR, ensure_ascii=False, indent=4, separators=(',', ':')))
    return 1


def custom_sort(item):
    """（原有代码保持不变）"""
    try:
        return int(item)
    except ValueError:
        return item


def main():
    """封装原来的主代码块为一个函数"""
    folder_abs_path = get_abs_path('output/Individual_Laboratory/')
    phase3_paths = []

    for root, dirs, files in os.walk(folder_abs_path):
        dirs.sort(key=custom_sort)
        for dir_name in dirs:
            if dir_name == 'phase3':
                phase3_paths.append(os.path.join(root, dir_name).replace('\\', '/'))

    prev_data = None
    index = 1
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
        npz_root = phase3_path + '/'

        npz_to_json(npz_root, '{}.json'.format(json_name))
        json_to_md('{}.json'.format(json_name), '{}.md'.format(markdown_name))


if __name__ == '__main__':
    # print("npz转md实验开始---")
    main()