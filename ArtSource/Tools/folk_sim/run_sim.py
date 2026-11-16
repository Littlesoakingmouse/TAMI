"""
Biên dịch và chạy bot mô phỏng mini-game trò chơi dân gian (Sim.cs + FolkModels.cs) bằng csc và dotnet đi kèm Unity.

  py -3 run_sim.py [số_ván_mỗi_trình_độ]
"""
import glob, json, os, subprocess, sys

HERE = os.path.dirname(os.path.abspath(__file__))
UNITY = r'E:\AR\Unity\6000.3.21f1\Editor\Data'
DOTNET = os.path.join(UNITY, 'NetCoreRuntime', 'dotnet.exe')
CSC = os.path.join(UNITY, 'DotNetSdkRoslyn', 'csc.dll')
FRAMEWORK = glob.glob(os.path.join(UNITY, 'NetCoreRuntime', 'shared', 'Microsoft.NETCore.App', '*'))[0]
MODELS = os.path.join(HERE, '..', '..', '..', 'Assets', '_Game', 'Scripts', 'FolkGames', 'FolkModels.cs')
OUT = os.path.join(os.environ.get('TEMP', HERE), 'folk_sim')

os.makedirs(OUT, exist_ok=True)
refs = [f for f in glob.glob(os.path.join(FRAMEWORK, '*.dll')) if os.path.basename(f).startswith(('System.', 'netstandard', 'mscorlib')) and 'Native' not in f]
dll = os.path.join(OUT, 'folk_sim.dll')
cmd = [DOTNET, CSC, '-nologo', '-nostdlib', '-target:exe', '-out:' + dll, '-langversion:9', '-optimize+']
cmd += ['-r:' + r for r in refs] + [os.path.join(HERE, 'Sim.cs'), os.path.abspath(MODELS)]
res = subprocess.run(cmd, capture_output=True, text=True)
if res.returncode != 0:
    print(res.stdout, res.stderr)
    sys.exit(1)
version = os.path.basename(FRAMEWORK)
with open(os.path.join(OUT, 'folk_sim.runtimeconfig.json'), 'w') as f:
    json.dump({'runtimeOptions': {'tfm': 'net6.0', 'framework': {'name': 'Microsoft.NETCore.App', 'version': version}}}, f)
sys.stdout.reconfigure(encoding='utf-8')
run = subprocess.run([DOTNET, dll] + sys.argv[1:], capture_output=True, text=True, encoding='utf-8')
print(run.stdout, run.stderr)
