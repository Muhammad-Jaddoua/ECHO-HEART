"""One command for the combined source/model/scene closeout checks. Does not launch Unity."""
from pathlib import Path
import argparse,hashlib,json,os,re,subprocess,sys,xml.etree.ElementTree as ET

ROOT=Path(__file__).resolve().parents[3]
TESTS=[
    ('flow','V1_3/Tests/FlowPolicyTests.csproj'),
    ('world_audio_chests','V1_5/Tests/WorldPresentationTests.csproj'),
    ('menu','V1_8/Tests/UiPolicyTests.csproj'),
    ('layout','V1_9/Tests/LayoutTests.csproj'),
    ('notifications','V1_10/Tests/NotificationTests.csproj'),
    ('guardian_feedback','V1_11/Tests/GuardianTests.csproj'),
    ('objectives','V1_12/Tests/GuideTests.csproj'),
    ('relic_glints','V1_13/Tests/GlintTests.csproj'),
    ('preferences','V1_14/Tests/PreferenceTests.csproj'),
    ('vitality','V1_15/Tests/VitalTests.csproj'),
    ('title_vista','V1_16/Tests/VistaTests.csproj'),
    ('chest_reveal','V1_20/Tests/RevealTests.csproj'),
    ('movement_cues','V1_21/Tests/MovementTests.csproj'),
    ('hurt_position','V1_23/Tests/HitPointTests.csproj'),
    ('counter_text','V1_25/Tests/CountTextTests.csproj'),
]

def stamp(project):
    files=[project,project.parent/'NuGet.Config']
    for node in ET.parse(project).getroot().iter('Compile'):
        if node.get('Include'):files.append((project.parent/node.attrib['Include']).resolve())
    result={}
    for path in files:
        if path==project.parent/'NuGet.Config' and not path.exists():continue
        path=path.resolve();name=path.relative_to(ROOT).as_posix()
        result[name]=hashlib.sha256(path.read_bytes()).hexdigest()
    return result

def main():
    p=argparse.ArgumentParser();p.add_argument('--dotnet',type=Path,required=True);p.add_argument('--report',type=Path,required=True)
    p.add_argument('--base-tree-json',type=Path);p.add_argument('--baseline',type=Path);args=p.parse_args()
    dotnet=args.dotnet.resolve();report=args.report.resolve();work=report.parent/(report.stem+'_evidence');work.mkdir(parents=True,exist_ok=True)
    env=os.environ.copy();env.update(DOTNET_CLI_TELEMETRY_OPTOUT='1',DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1')
    def run(command):
        result=subprocess.run([str(x) for x in command],cwd=ROOT,env=env,text=True,capture_output=True)
        if result.returncode:raise RuntimeError((' '.join(str(x) for x in command)+'\n'+result.stdout+'\n'+result.stderr)[-9000:])
        return result.stdout.strip()
    try:
        manifest=work/'manifest.json';builder=[sys.executable,ROOT/'Tools/Jehad/V1_27/build_checkpoint.py','--manifest',manifest]
        if args.base_tree_json:builder+=['--base-tree-json',args.base_tree_json.resolve()]
        run(builder);second=json.loads(run(builder));assert second['written']==0,second
        sdk=run([dotnet,'--version']);compiled={};benchmark=None
        for label,relative in TESTS:
            project=ROOT/'Tools/Jehad'/relative
            output=run([dotnet,'run','--project',project,'--configuration','Release'])
            match=re.search(r'PASS: (\d+) assertions',output);assert match,output
            compiled[label]={'result':'PASS','project':project.relative_to(ROOT).as_posix(),'assertions':int(match[1]),'input_sha256':stamp(project),'output':output}
            (work/(label+'.txt')).write_text(output+'\n')
            if label=='counter_text':benchmark=json.loads(next(line for line in output.splitlines() if line.startswith('{')))
            print(f"PASS {label}: {match[1]} assertions",flush=True)
        regression={'result':'PASS','dotnet_sdk':sdk,'suite_count':len(compiled),'total_assertions':sum(x['assertions'] for x in compiled.values()),'compiled_tests':compiled,'managed_counter_microbenchmark':benchmark,'scope':'Actual pure C# sources only; no Unity compilation, stubs or native execution.'}
        regression_path=work/'regression.json';regression_path.write_text(json.dumps(regression,indent=2)+'\n')
        namespace_path=work/'namespaces.json'
        run([dotnet,'run','--project',ROOT/'Tools/Jehad/V1_22/NamespaceAudit/NamespaceAudit.csproj','--configuration','Release','--',ROOT,namespace_path])
        validator=[sys.executable,ROOT/'Tools/Jehad/V1_27/validate_checkpoint.py','--manifest',manifest,'--namespace-report',namespace_path,'--regression-report',regression_path,'--report',report]
        if args.base_tree_json:validator+=['--base-tree-json',args.base_tree_json.resolve()]
        if args.baseline:validator+=['--baseline',args.baseline.resolve()]
        run(validator)
        checked=json.loads(report.read_text());checked['builder_second_run_files_written']=0
        report.write_text(json.dumps(checked,indent=2)+'\n')
        print(json.dumps({'result':'PASS','suites':len(compiled),'assertions':regression['total_assertions'],'report':str(report),'unity_runtime':'NOT RUN'}),flush=True)
        return 0
    except Exception as error:
        report.parent.mkdir(parents=True,exist_ok=True);report.write_text(json.dumps({'result':'FAIL','error':str(error),'unity_runtime':'NOT RUN'},indent=2)+'\n')
        print(str(error),file=sys.stderr);return 1

if __name__=='__main__':sys.exit(main())
