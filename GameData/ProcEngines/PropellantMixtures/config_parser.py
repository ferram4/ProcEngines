from os import walk

files = []
for (dirpath, dirnames, filenames) in walk('./data/'):
    files.extend(filenames)
    break

data = []
OFratio = None
for file in files:
    t = []
    with open('./data/' + file) as f:
        for i, line in enumerate(f):
            if i in [15, 24, 25, 29, 31, 34, 39]:
                t.append(line.split())

    OFratio = t[0][2]
    Pc = t[1][1]
    Tc = t[2][1]
    Te = t[2][4]
    Pe = t[1][4]
    MW = t[5][4]
    gamma = t[4][4]
    Mach = t[6][5]
    Cpc = t[3][3]
    Cpe = t[3][6]

    data.append([Pc, Tc, Te, Pe, MW, gamma, Mach, Cpc, Cpe])

if len(data) < 15:
    print('[WRN] Less than 15 keys!')

block = ''.join(['MixtureRatioData\n{\n  OFratio =', OFratio,
        '\n  PressureData\n  {\n',
        ''.join(['    key = {}, {}, {}, {}, {}, {}, {}, {}, {}\n'.format(*line) for line in data]),
        '  }\n}'])

with open('./data/results.txt', 'a') as f:
    f.write(block)
