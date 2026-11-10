#!/usr/bin/env python3
"""Baseline a local, trusted-proxy endpoint without printing session tokens."""
import concurrent.futures
import http.client
import json
import os
import statistics
import time

host = os.environ.get('TAURUS_MEASURE_HOST', '127.0.0.1:5080')
application_host = os.environ['TAURUS_MEASURE_APPLICATION_HOST']
token = os.environ['TAURUS_MEASURE_SESSION']
count = int(os.environ.get('TAURUS_MEASURE_COUNT', '200'))
concurrency = int(os.environ.get('TAURUS_MEASURE_CONCURRENCY', '1'))


def request(_):
    connection = http.client.HTTPConnection(host)
    start = time.perf_counter()

    connection.request('GET', '/_taurus/auth', headers={
        'Host': application_host,
        'X-Forwarded-Proto': 'https',
        'Cookie': '__Host-TaurusSession=' + token
    })
    response = connection.getresponse()
    response.read()
    connection.close()

    assert response.status == 204, response.status

    return (time.perf_counter() - start) * 1000


for warmup in range(10):
    request(warmup)

start = time.perf_counter()

with concurrent.futures.ThreadPoolExecutor(max_workers=concurrency) as executor:
    timings = sorted(executor.map(request, range(count)))

elapsed = time.perf_counter() - start
result = {
    'requests': count,
    'concurrency': concurrency,
    'elapsed_seconds': elapsed,
    'requests_per_second': count / elapsed,
    'median_ms': statistics.median(timings),
    'p95_ms': timings[min(len(timings) - 1, int(len(timings) * 0.95))]
}

print(json.dumps(result, indent=2))
