import http from 'k6/http';
import { check, sleep } from 'k6';

export let options = {
    stages: [
        { duration: '10s', target: 10 }, // ramp up to 10 users
        { duration: '30s', target: 10 }, // stay at 10 users
        { duration: '10s', target: 0 },  // ramp down
    ],
};

export default function () {
    const url = 'http://api:8080/api/v1/auth/login';
    const payload = JSON.stringify({
        username: 'test-user',
        password: 'my_test_password',
    });

    const params = {
        headers: {
            'Content-Type': 'application/json',
        },
    };

    const res = http.post(url, payload, params);
    check(res, {
        'status is 200': (r) => r.status === 200,
        'response time < 500ms': (r) => r.timings.duration < 500,
    });
    console.log('Response status:', res.status);
    console.log('Response body:', res.body);

    if (!success && __ENV.FAIL_ON_CHECK_FAIL === 'true') {
        fail('At least one check failed — aborting for CI signal.');
    }
    
    sleep(1);
}
