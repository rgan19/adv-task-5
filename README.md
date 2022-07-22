# Adv Task 5


SAGA Pattern from microservices.io

![Image](https://chrisrichardson.net/i/sagas/Create_Order_Saga.png)


### Create task
For task producer (project name: Task), use docker image `rgan19/task-producer:2.0.0`

- Create a task in `task` queue - orders are tracked by taskID

```
{
    "taskID": 1001,
    "customerID": 2000,
    "description": "Order food",
    "priority": "low",
    "status": 0
}
```

### Consume task

- In Task Processor Service (project name: TaskProcessor), use docker image `rgan19/task-consumer:2.0.0`
- After running `consumer-deploy.yaml`, type this into url: http://localhost:31289/swagger/index.html
- GET request will read all tasks in rabbitmq `tasks` queue


### Update task status
- POST request will add task to `task-processed` queue to change status of task to completed

```
{
    "taskID": 1001,
    "customerID": 2000,
    "description": "Order food",
    "priority": "low",
    "status": 2
}
```

#### Reference: Status numbers
| number | status     | 
|---|---------------|
| 0 | "STARTED"     |
| 1 | "IN_PROGRESS" |
| 2 | "COMPLETED"   |
| 3 | "FAILED"      |