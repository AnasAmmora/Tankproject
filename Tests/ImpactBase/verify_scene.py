"""Assert the saved GameScene diff is restricted to the two base visuals."""
import pathlib, re, subprocess
root=pathlib.Path(__file__).resolve().parents[2]
path='Assets/ProjectsFiles/Scenes/GameScene.unity'
before=subprocess.check_output(['git','show','HEAD:'+path],cwd=root,text=True)
after=(root/path).read_text()
before='\n'.join(line.rstrip() for line in before.splitlines())+'\n'
after='\n'.join(line.rstrip() for line in after.splitlines())+'\n'
def blocks(text):
    return {int(m[2]):(int(m[1]),m[0]) for m in re.finditer(r'^--- !u!(\d+) &(\d+)[^\n]*\n.*?(?=^--- !u!|\Z)',text,re.M|re.S)}
old,new=blocks(before),blocks(after)
assert not old.keys()-new.keys(), 'An original scene object was removed'
changed=[]
for key,(kind,body) in old.items():
    current=new[key][1]
    if body==current: continue
    changed.append(key)
    if kind==23:
        assert re.search(r'm_GameObject: \{fileID: (190001000|1514753388)\}',body)
        assert current==body.replace('m_Enabled: 1','m_Enabled: 0',1)
    elif kind==4:
        assert key in (190001006,1514753394)
        normalized=re.sub(r'  m_Children:\n  - \{fileID: \d+\}', '  m_Children: []',current)
        assert normalized==body
    elif kind==1001:
        # Exactly two appended renderer references and their array-size override.
        pattern=(r'    - target: \{fileID: 7734147480763793029, guid: [^\n]+\n'
                 r"      propertyPath: '?sceneRenderers.Array.(?:size|data\[(?:65|66)\])'?\n"
                 r'      value: ?[^\n]*\n      objectReference: \{fileID: \d+\}\n')
        normalized,count=re.subn(pattern,'',current)
        assert count==3 and normalized==body
    else: raise AssertionError((key,kind,'Unexpected gameplay/environment change'))
assert len(changed)==5, changed
added=[new[k][0] for k in new.keys()-old.keys()]
assert sorted(added)==[4,4,23,23,1001,1001],added
print('PASS: all original scene objects retained; only two visual visibility flags, two child lists, and CurvedWorld renderer references changed; six visual prefab records added.')
