"""Read the saved prefab. Do not launch Unity or write assets."""
import re
import subprocess
from pathlib import Path

root = Path(__file__).resolve().parents[2]
asset = 'Assets/ProjectsFiles/Prefabes/Player.prefab'
text = (root / asset).read_text(encoding='utf-8-sig')

def documents(value):
    entries = re.findall(r'^--- !u!(\d+) &(\d+)[^\n]*\n(.*?)(?=^--- !u!|\Z)', value, re.M | re.S)
    assert len(entries) == len({entry[1] for entry in entries}), 'Duplicate local file IDs'
    return {file_id: (int(kind), body) for kind, file_id, body in entries}

docs = documents(text)
for file_id, (_, body) in docs.items():
    for reference in re.findall(r'\{fileID: (\d+)\}', body):
        assert reference == '0' or reference in docs, f'Unresolved local reference {reference} in {file_id}'

guid = re.search(r'^guid: (\w+)', (root / 'Assets/ProjectsFiles/Scripts/Player/TankRecoil.cs.meta').read_text(), re.M)[1]
recoils = [(key, body) for key, (_, body) in docs.items() if f'guid: {guid},' in body]
assert len(recoils) == 1
recoil_id, recoil = recoils[0]
assert 'm_GameObject: {fileID: 6930759457325569799}' in recoil
assert f'component: {{fileID: {recoil_id}}}' in docs['6930759457325569799'][1]
assert 'movement: {fileID: 744912970748391413}' in recoil
assert 'IsActive: 1\n' in recoil
assert float(re.search(r'recoilForce: ([\d.]+)', recoil)[1]) >= 0, 'Preserve user-tuned recoil force'
assert f'recoil: {{fileID: {recoil_id}}}' in docs['4834099022764106920'][1]
assert sum(kind == 54 for kind, _ in docs.values()) == 1, 'Keep the single Rigidbody'

original = documents(subprocess.check_output(['git', 'show', 'HEAD:' + asset], cwd=root, text=True))
assert set(docs) - set(original) == {recoil_id}
assert not set(original) - set(docs)
for key, value in original.items():
    current = docs[key]
    if key == '6930759457325569799':
        current = (current[0], current[1].replace(f'  - component: {{fileID: {recoil_id}}}\n', ''))
    elif key == '4834099022764106920':
        current = (current[0], current[1].replace(f'  recoil: {{fileID: {recoil_id}}}\n', ''))
    elif key == '744912970748391413':
        assert 'anticipateFiringRecoil: 1\n' in current[1]
        current = (current[0], current[1].replace('  anticipateFiringRecoil: 1\n', ''))
    elif key == '1326213804012113766':
        kind, body = current
        assert 'Assembly-CSharp::TankNetworkTransform\n' in body
        assert 'PositionInterpolationType: 0\n' in body and 'PositionLerpSmoothing: 0\n' in body
        assert 'Interpolate: 1\n' in body and 'AuthorityMode: 1\n' in body
        body = body.replace('38448713bfc644e688f2271e1b087862', 'e96cb6065543e43c4a752faaa1468eb1')
        body = body.replace('Assembly-CSharp::TankNetworkTransform', 'Unity.Netcode.Runtime::Unity.Netcode.Components.NetworkTransform')
        body = body.replace('PositionInterpolationType: 0\n', 'PositionInterpolationType: 1\n')
        body = body.replace('PositionLerpSmoothing: 0\n', 'PositionLerpSmoothing: 1\n')
        body = body.replace('  logMotionTiming: 0\n', '')
        current = (kind, body)
    elif key == '6666415672332137553' and current != value:
        # NGO OnValidate normalizes the prefab-stage hash to the asset GlobalObjectId.
        # No ownership/transform/network setting may change along with it.
        assert 'GlobalObjectIdHash: 93640355\n' in current[1]
        current = (current[0], current[1].replace('GlobalObjectIdHash: 93640355\n', 'GlobalObjectIdHash: 4181086163\n'))
    assert current == value, f'Unrelated prefab data changed: {key}'
print('PASS: saved recoil/motion references, unique IDs, single Rigidbody, authored interpolation profile; other prefab data preserved.')
