#!/usr/bin/env python3
"""Render shipped proxy snippets for a different Taurus request prefix."""
import argparse
from pathlib import Path
import re

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--prefix', required=True)
parser.add_argument('--output', type=Path, required=True)
arguments = parser.parse_args()
prefix = arguments.prefix

if prefix and not re.fullmatch(r'/[A-Za-z0-9_-]+(?:/[A-Za-z0-9_-]+)*', prefix):
    parser.error('prefix must be empty or slash-separated safe path segments')

source = Path(__file__).resolve().parent.parent / 'deployment'
arguments.output.mkdir(parents=True, exist_ok=True)

for filename in ('nginx.conf', 'caddy.conf', 'traefik.yml'):
    content = (source / filename).read_text()

    if not prefix:
        if filename == 'nginx.conf':
            content = content.replace('location /_taurus/ {',
                r'location ~ ^/(?:ui(?:/|$)|logout$|taurus\.v1\.'
                r'(?:Authentication|Administration)/) {')
        elif filename == 'caddy.conf':
            content = content.replace('handle /_taurus/* {',
                '@taurus path /ui /ui/* /logout /taurus.v1.Authentication/* '
                '/taurus.v1.Administration/*\n'
                '        handle @taurus {')
        else:
            content = content.replace('PathPrefix(`/_taurus/`)',
                '(Path(`/ui`) || Path(`/logout`) || PathPrefix(`/ui/`) || '
                'PathPrefix(`/taurus.v1.Authentication/`) || '
                'PathPrefix(`/taurus.v1.Administration/`))')

    content = content.replace('/_taurus', prefix)
    destination = arguments.output / filename

    if destination.exists():
        parser.error(f'refusing to overwrite {destination}')

    destination.write_text(content)
