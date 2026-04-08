from dlisio import dlis
from dlisio import lis
from dlisio.lis.curves import dfsr_fmtstr, dfsr_dtype, validate_dfsr

import numpy as np
import os

#выводит список кривых из lis файла
#file - путь к lis
def load_lis(file):    
    with lis.load(file) as files:
        print('-------------')
        print('NAME \t TYPE')
        print('-------------')
        for f in files:
            for format_spec in f.data_format_specs():
                curves = lis.curves(f, format_spec)
                
                name_to_type = dfsr_dtype(format_spec, sample_rate=1)
                for name in curves.dtype.names:
                    print(name + '\t' + name_to_type[name].name)


#выводит список кривых из dlis файла
#file - путь к dlis
def load_dlis(file):
    with dlis.load(file) as files:
        print('-------------')
        print('NAME')
        print('-------------')
        for f in files:
            for format_spec in f.data_format_specs():
                curves = lis.curves(f, format_spec)
                
                for name in curves.dtype.names:
                    print(name)

#выводит данные по кривой из lis файла
#file - путь к lis
#cname - кривая по которой будут выведены данные
#dname - кривая глубины
#r - кол-во строк данных
def load_curve_data(file, cname, dname, r = None):
    with lis.load(file) as files:
        print('-------------')
        print(dname + '\t' + cname)
        print('-------------')
        for f in files:
            for format_spec in f.data_format_specs():
                curves = lis.curves(f, format_spec)
                
                for cdata in curves[[dname, cname]][0:r]:
                    print(str(cdata[0]) + '\t' + str(cdata[1]))


def load_any(file):
    _, ext = os.path.splitext(file)
    if ext == '.lis':
        load_lis(file)
    elif ext == '.dlis':
        load_dlis(file)
    else:
        print('Unsupport file extension ' + ext)



#путь к dlis/lis файлу
file = 'АКСТ.lis'

print('Read file: ' + file)
load_any(file)

#кривая данные по которой мы хотим увидеть
cname = 'AKCT'

#кривая глубины
dname = 'DEPT'

#кол-во строк данных, которые будут выведены
r = 1500

load_curve_data(file, cname, dname)
            