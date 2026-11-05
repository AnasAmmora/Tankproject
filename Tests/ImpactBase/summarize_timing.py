import pathlib, re, csv, json
folder=pathlib.Path(__file__).resolve().parent
rows=[]
for label in ['baseline','fixed','fixed100','surfaces100']:
    for peer in ['host','client']:
        events={}
        for line in (folder/f'{label}-{peer}.log').read_text().splitlines():
            match=re.search(r'shot=(\d+:\d+).*?stage=(\S+).*?localMark=([\d.]+)(?:.*?frame=(\d+))?',line)
            if not match:continue
            shot,stage,mark,frame=match.groups()
            events.setdefault(shot,{}).setdefault(stage,[]).append((float(mark),int(frame) if frame else None))
        for shot,stages in events.items():
            def get(stage):return stages.get(stage,[(None,None)])[0]
            contact,cf=get('projectile-contact');confirmed,_=get('projectile-result-impact');effect,ef=get('impact-effect-played')
            fallback,ff=get('projectile-confirmed-contact')
            effects=len(stages.get('impact-effect-played',[]))
            if label!='baseline':assert effects==1,(label,peer,shot,effects)
            if cf is not None and ef is not None:assert cf==ef,(label,peer,shot,'Effect slipped to a later frame')
            rows.append(dict(run=label,peer=peer,shot=shot,contact=contact,confirmation=confirmed,effect=effect,
                confirmation_gap_ms=round((confirmed-contact)*1000,2) if contact and confirmed else '',
                effect_gap_ms=round((effect-contact)*1000,2) if contact and effect else '',
                contact_frame=cf if cf is not None else '',effect_frame=ef if ef is not None else '',
                confirmed_fallback=fallback is not None,effect_count=effects))
with (folder/'timing.csv').open('w',newline='') as f:
    writer=csv.DictWriter(f,fieldnames=rows[0].keys());writer.writeheader();writer.writerows(rows)
assert len(rows)==24,len(rows)
print(json.dumps(rows,indent=2))
print('PASS: 20 changed-version peer/shot presentations, exactly one effect each; raw same-peer timing retained in timing.csv.')
