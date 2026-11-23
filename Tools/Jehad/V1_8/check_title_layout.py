"""Read literal title rectangles from actual C# syntax; this is not a Unity rendering test."""
from pathlib import Path
import json
from tree_sitter import Language,Parser
import tree_sitter_c_sharp

def title_geometry(source):
    data=source.read_bytes();parser=Parser(Language(tree_sitter_c_sharp.language()));tree=parser.parse(data)
    assert not tree.root_node.has_error
    def text(node):return data[node.start_byte:node.end_byte].decode()
    def walk(node):
        yield node
        for child in node.named_children:yield from walk(child)
    rectangles=[]
    for node in walk(tree.root_node):
        if node.type!='invocation_expression':continue
        function=node.child_by_field_name('function');arguments=node.child_by_field_name('arguments')
        if function is None or arguments is None:continue
        method=text(function);args=[text(x) for x in arguments.named_children if x.type=='argument']
        if method not in {'_ui.Label','_ui.Button'} or not args or args[0]!='Title':continue
        kind='button' if method.endswith('Button') else 'text'
        label=json.loads(args[1]);x,y,width=map(float,args[2:5])
        height=float(args[7]) if kind=='button' and len(args)>7 else 62 if kind=='button' else float(args[5])
        rectangles.append({'kind':kind,'label':label,'rect':[x,-y,width,height]})
    return rectangles

def overlap(a,b):
    ax,ay,aw,ah=a;bx,by,bw,bh=b
    return min(ax+aw,bx+bw)>max(ax,bx)+.01 and min(ay+ah,by+bh)>max(ay,by)+.01

def inspect(source):
    rects=title_geometry(source);buttons=[r for r in rects if r['kind']=='button'];collisions=[]
    assert len(buttons)==5,'Expected Begin Adventure, Settings, Controls, Credits and Quit'
    for r in rects:
        x,y,w,h=r['rect'];assert x>=0 and y>=0 and x+w<=1320 and y+h<=760,('outside title card',r)
    for i,a in enumerate(rects):
        for b in rects[i+1:]:
            if 'button' in {a['kind'],b['kind']} and overlap(a['rect'],b['rect']):collisions.append([a['label'],b['label']])
    return {'direct_title_rectangles':len(rects),'buttons':buttons,'button_text_or_button_overlaps':collisions}

def validate(root):
    old=inspect(root/'Assets/_Project/Scripts/UI/Jehad/V1_7/MenuScreensV17.cs')
    current=inspect(root/'Assets/_Project/Scripts/UI/Jehad/V1_8/MenuScreensV18.cs')
    assert any('Quit' in pair for pair in old['button_text_or_button_overlaps']),'Previous overlap was not reproduced'
    assert not current['button_text_or_button_overlaps'],current['button_text_or_button_overlaps']
    return {'result':'PASS','scope':'Literal C# title rectangles only; excludes actual font rendering, transforms and runtime input','previous_quit_overlap_reproduced':True,**current}

if __name__=='__main__':print(json.dumps(validate(Path(__file__).resolve().parents[3]),indent=2))
