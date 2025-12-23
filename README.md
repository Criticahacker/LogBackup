# Log Backup Worker Service

Background worker service that incrementally backs up application log files with masking, offset tracking, and parallel processing for high-volume environments.

## Features
- Incremental log backup
- Masking & transformation
- Parallel processing
- Crash-safe offset persistence
- Configurable & scalable

## Architecture
Core + Infrastructure + Worker separation

## Future Scope

Currently, the system focuses on reliable and incremental log backup, ensuring every file is processed safely with offset tracking and masking. However, in real-world production environments, multiple applications continuously generate logs, and once files reach a configured max size, new log files are created. Over time, this leads to:
•	A continuously growing number of input log files
•	Larger state.json as more files are tracked
•	Slower scanning because every file must still be discovered and evaluated

To keep the system efficient, maintainable, and production-ready at scale, a Log Archival & Cleanup Module can be added as a future enhancement.

Once a log file is fully processed and backed up, it should be:
	Archived or deleted to prevent unlimited file growth
	Removed from state.json to keep state small and improve performance

## Tech Stack
.NET | Serilog | Async Parallel Processing
