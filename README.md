## Design Decisions

### A. How did you define and enforce overlapping bookings, and why?

Two bookings overlap when they are for the same resource and their time ranges intersect.

I used this condition:

`NewStart < ExistingEnd && NewEnd > ExistingStart`

I only check active bookings because cancelled bookings should not prevent a new booking. I used this approach because it is simple and correctly handles different types of time overlaps.

### B. What did you assume about concurrency?

I assumed that two users could try to book the same resource at almost the same time.

Because of this, checking for an existing booking and creating the new booking should happen safely together. I used a Serializable transaction in SQL Server to reduce the chance of two concurrent requests creating overlapping bookings.

### C. What would break in your design at scale, and where would the first bottleneck be?

The database would probably be the first bottleneck.

A high number of concurrent booking requests could cause more database connections, locking, and transaction contention, especially because I use Serializable transactions for the booking operation.

### D. How would you evolve this into a distributed system?

I would keep the API stateless so multiple API instances could run behind a load balancer.

For example, I could add Redis for caching and RabbitMQ for operations that do not need to happen immediately. The database would still be responsible for making sure that two users cannot successfully book the same resource and time.

### E. Which tradeoff did you prioritize — simplicity, correctness, or performance, and why?

I prioritized **correctness**.

For a booking system, preventing two users from booking the same resource at the same time is more important than getting the highest possible performance. I chose a relatively simple implementation using SQL Server transactions to make the booking operation safer under concurrency.
