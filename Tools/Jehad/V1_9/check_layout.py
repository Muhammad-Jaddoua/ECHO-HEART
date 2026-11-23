"""Reuse the title rectangle parser against the current version's actual composition."""
from pathlib import Path
import sys
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'V1_8'))
from check_title_layout import inspect

def validate(root):
    report=inspect(root/'Assets/_Project/Scripts/UI/Jehad/V1_9/MenuScreensV19.cs')
    assert not report['button_text_or_button_overlaps'],report
    return {'result':'PASS','scope':'Literal title rectangles only; actual rendering remains untested',**report}
