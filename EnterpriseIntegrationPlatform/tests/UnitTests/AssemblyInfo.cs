using NUnit.Framework;

// Run test fixtures in parallel — each fixture gets its own thread.
// Individual tests within a fixture remain sequential (ParallelScope.Fixtures).
[assembly: Parallelizable(ParallelScope.Fixtures)]
