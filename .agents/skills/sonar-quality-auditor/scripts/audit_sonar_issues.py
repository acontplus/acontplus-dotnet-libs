#!/usr/bin/env python3
import json
import os
import sys
from collections import defaultdict, Counter

EXCLUDED_RULES = {
    'secrets:S6693', 'secrets:S6697', 'secrets:S6702', 'secrets:S6692',
    'secrets:S6290', 'csharpsquid:S6418', 'csharpsquid:S2068'
}

CANDIDATE_PATHS = [
    '.sonarqube-results/latest/issues.json',
    '.sonarqube-results/issues.json',
    '.sonarqube/issues.json',
    'acontplus-sonarqube-results/issues.json',
    'issues.json'
]

def find_issues_file():
    if len(sys.argv) > 1 and os.path.isfile(sys.argv[1]):
        return os.path.abspath(sys.argv[1])
    # Look in current working dir and up to repo root
    cwd = os.getcwd()
    search_dirs = [cwd, os.path.dirname(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))))]
    for base in search_dirs:
        for p in CANDIDATE_PATHS:
            full = os.path.join(base, p)
            if os.path.isfile(full):
                return full
    return None

def is_excluded_component(component, rule):
    comp_lower = component.lower()
    if 'appsettings' in comp_lower or 'bundleconfig' in comp_lower:
        return True
    if '/migrations/' in comp_lower:
        return True
    if comp_lower.endswith(('.html', '.json')):
        return True
    if rule in EXCLUDED_RULES:
        return True
    return False

def audit():
    file_path = find_issues_file()
    if not file_path:
        print('Error: No issues.json or .sonarqube/issues.json found.', file=sys.stderr)
        sys.exit(1)

    print(f'Auditing issues from: {file_path}')
    with open(file_path, 'r', encoding='utf-8') as f:
        data = json.load(f)

    issues = data.get('issues', [])
    total_issues = len(issues)
    actionable = []

    for issue in issues:
        comp = issue.get('component', '').replace('acontplus-dotnet-libs:', '')
        rule = issue.get('rule', '')
        if is_excluded_component(comp, rule):
            continue
        actionable.append(issue)

    print(f'Total issues: {total_issues}')
    print(f'Actionable backend issues: {len(actionable)}')
    print('-' * 60)

    by_severity = Counter(i.get('severity') for i in actionable)
    for sev, cnt in by_severity.most_common():
        print(f'  {sev}: {cnt}')
    print('-' * 60)

    grouped = defaultdict(list)
    for i in actionable:
        comp = i.get('component', '').replace('acontplus-dotnet-libs:', '')
        grouped[comp].append(i)

    for comp, iss in sorted(grouped.items(), key=lambda x: -len(x[1])):
        print(f'{comp} ({len(iss)} issues):')
        for item in iss[:5]:
            print(f"    L{item.get('line', '?')} [{item.get('rule')}] {item.get('message')}")
        if len(iss) > 5:
            print(f"    ... and {len(iss) - 5} more")

if __name__ == '__main__':
    audit()
