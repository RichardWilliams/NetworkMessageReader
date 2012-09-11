This is my first attempt at trying out the Rx and FromAsyncPattern.

I am not happy with the way I have done the testing, as I am using a ManualResetEventSlim. 
When the stream has completed(its been closed) then the OnCompleted will be called and 
here it sets the ManualResetEventSlim. So my tests block until the Observable has completed.

I have tried to get my head around how to use the Schedulers for this, but I am really not sure how
I can put them in with this scenario as I am passing in an TcpClient and get an Observable returned. 
If someone can write a similar to test to what I have done without the ManualResetEvent 
and show me how I put in the Scheduler that would be superb!

Anyway enjoy :-)