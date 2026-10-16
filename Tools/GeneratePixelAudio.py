"""Original chiptune SFX. Standard-library synthesis; no downloaded samples."""
from pathlib import Path
import math, random, struct, wave

ROOT = Path(__file__).resolve().parents[1] / 'Assets' / 'Audio'
ROOT.mkdir(parents=True, exist_ok=True)
RATE = 22050
SOUNDS = {
    'Pickup': (.16, [660, 990, 1320], .02),
    'Hit': (.075, [520, 260], .25),
    'Hurt': (.22, [220, 120, 70], .3),
    'Clash': (.16, [1700, 1200, 700], .6),
    'Shield': (.3, [440, 880, 660], .02),
    'Upgrade': (.5, [523, 659, 784, 1047], 0),
    'Wave': (.13, [1000, 650, 380], .12),
    'Mana': (.38, [880, 1175, 1568], 0),
    'Refill': (.42, [392, 523, 659, 784, 1047], .04),
    'RoundTable': (.65, [220, 330, 440, 660], .12),
    'Charge': (1.0, [110, 165, 220, 330, 440, 660, 880], .04),
    'Holy': (.65, [1047, 784, 523, 262], .35),
    'Boss': (.7, [196, 185, 147, 98], .16),
    'Death': (.65, [392, 330, 262, 196, 98], .1),
}

for index, (name, (duration, notes, noise)) in enumerate(SOUNDS.items()):
    rng = random.Random(800 + index)
    frames, phase = [], 0
    count = round(duration * RATE)
    for i in range(count):
        t = i / RATE
        note = notes[min(len(notes) - 1, int(t / duration * len(notes)))]
        phase += note / RATE
        triangle = 1 - 4 * abs((phase % 1) - .5)
        square = 1 if phase % 1 < .25 else -1 / 3
        tone = .7 * triangle + .3 * square
        envelope = min(1, t / .006) * min(1, (duration - t) / .04)
        envelope *= (.5 + .5 * t / duration) if name == 'Charge' else math.exp(-2.3 * t / duration)
        value = ((1 - noise) * tone + noise * rng.uniform(-1, 1)) * envelope
        frames.append(value)
    peak = max(abs(x) for x in frames)
    pcm = b''.join(struct.pack('<h', round(x / peak * 22000)) for x in frames)
    with wave.open(str(ROOT / (name + '.wav')), 'wb') as output:
        output.setparams((1, 2, RATE, count, 'NONE', 'not compressed'))
        output.writeframes(pcm)
    assert peak > 0 and abs(frames[-1]) < .01
print(f'Generated {len(SOUNDS)} original mono PCM effects in {ROOT}')
