import math

# MetaForVVVV: PerlinAtPoint.ihm.json

def veclength(invec):
    s = 0.0
    for i in range(0, len(invec)):
        s += invec[i] ** 2
    return s ** 0.5;

def lerp(a, b, t):
    return a+(b-a)*t

def n1(p):
    tempv = p
    for i in range(0, 3):
        tempv[i] = math.sin(p[i] * (3.3 ** 0.5) + math.fmod(math.sin(p[2-i] * (5.0 ** 0.5) + Options.Content["RandomSeed"][0]) * (8.0 ** 0.5), 1.0) * 8.0)
    return math.cos(veclength(tempv))

def octave(n):
    ii = VVVVContext.InvokationCount % Options.Content["Rows"][0]
    ti = VVVVContext.ThreadID
    x = [PosList.Content["X"][ii], PosListY.Content["Y"][ti]]
    z = Options.Content["Z"][0] / ((2.0 ** MorphBalance) ** n - 6.0)
    p = [(x[0] + 3.0) * (1111.0 ** 0.5), (x[0] + 3.0) * (1111.0 ** 0.5), math.floor(z)]
    c = lerp(n1(p), n1([p[0], p[1], p[2] + 1.0]), math.fmod(z, 1.0))

def pblend():
    s = 0.0
    sc = 0
    for i in range(0, Options.Content["Octaves"][0]):
        sc = i
        s += octave(i)
    s /= sc
    s *= Options.Content["Amplitude"][0] * (4.0 / (2.0 ** (3.4 * math.fabs(Options.Content["FrequencyBalance"][0]))))
    return s

def execute():
    rows = Options.Content["Rows"][0]
    cols = Options.Content["Columns"][0]
    ii = VVVVContext.InvokationCount % rows
    ti = VVVVContext.ThreadID
    Result.Content["Values"][ti * rows + ii] = pblend()

    if (ii == (rows - 1)) and Options.Content["WaitForController"][0]:
        VVVVContext.BreakRequest = true
        Result.Content["RowDone"][ti] = true
execute()
