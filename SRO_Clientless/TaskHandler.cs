using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace SRO_Clientless
{
    public class TaskHandler
    {
        public static ITargetBlock<DateTimeOffset> CreateNeverEndingTask(Func<DateTimeOffset, CancellationToken, Task> action, CancellationToken cancellationToken, int milisecondsDelay)
        {
            // Validate parameters.
            if (action == null) throw new ArgumentNullException("action");

            ActionBlock<DateTimeOffset> block = null;

            block = new ActionBlock<DateTimeOffset>(async now =>
            {
                // Perform the action.  Wait on the result.
                await action(now, cancellationToken).
                    ConfigureAwait(false);

                // Wait.
                await Task.Delay(milisecondsDelay, cancellationToken).
                    // Same as above.
                    ConfigureAwait(false);

                // Post the action back to the block.
                block.Post(DateTimeOffset.Now);
            }, new ExecutionDataflowBlockOptions
            {
                CancellationToken = cancellationToken
            });

            // Return the block.
            return block;
        }
    }
}
