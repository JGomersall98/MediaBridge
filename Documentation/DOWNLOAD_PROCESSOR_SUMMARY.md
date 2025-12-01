# Download Processor Service - Implementation Summary

## Overview
I have completely refactored your `DownloadProcessorService` to properly handle Sonarr queue monitoring and episode tracking. The service now runs as a background service every 30 seconds and follows your exact requirements.

## Key Components Implemented

### 1. Background Service (New)
- **File:** `Services/Background/DownloadQueueBackgroundService.cs`
- **Function:** Runs every 30 seconds automatically
- **Behavior:** 
  - Only processes if there are active downloads (`Status = "queued"` or `"downloading"`)
  - Gracefully handles errors and logs them
  - Uses proper dependency injection scoping

### 2. Enhanced DownloadProcessorService
- **File:** `Services/Media/Downloads/DownloadProcessorService.cs`
- **Key Improvements:**
  - Complete rewrite with proper error handling
  - Efficient API call batching (groups by series to minimize requests)
  - Updates existing downloads with current progress
  - Adds missing episodes automatically
  - Proper status mapping from Sonarr to your database

### 3. Configuration Setup
- **Migration:** Added `sonarr_episode_data_endpoint` configuration
- **Required Config Keys:**
  - `sonarr_download_queue_endpoint` (existing)
  - `sonarr_episode_data_endpoint` (new)
  - `sonarr_api_key` (existing)

## How It Works

### Step 1: Trigger & Timing
- Background service runs every 30 seconds
- First checks if there are any active downloads in your database
- If none found, exits early (no unnecessary API calls)

### Step 2: Fetch Sonarr Queue
- Calls Sonarr queue endpoint: `GET /api/v3/queue?apikey={key}&pageSize=1000`
- Parses all download items from the response

### Step 3: Process Queue Items
- Groups items by series to minimize API calls
- For each series group:
  - Updates existing episode downloads with current progress
  - Identifies missing episodes not yet in database
  - Fetches episode metadata for missing episodes
  - Creates new `DownloadRequests` entries

### Step 4: Data Mapping
For each episode, the service creates/updates:
- **MediaId:** Sonarr series ID
- **EpisodeId:** Sonarr episode ID
- **TvdbId:** TheTVDB ID for cross-referencing
- **Title:** Episode title with season/episode format (e.g., "Episode Title - S01E05")
- **Status:** Mapped from Sonarr status
- **DownloadPercentage:** Calculated from size/sizeLeft
- **MinutesLeft:** Parsed from Sonarr's timeLeft field
- **SeasonNumber:** Season number
- **UserId:** Inherited from parent series request

### Step 5: Status Mapping
| Sonarr Status | Your DB Status |
|---------------|----------------|
| downloading   | downloading    |
| queued        | queued         |
| paused		| paused         |
| completed     | completed		 |
| failed        | failed         |
| importpending | importing      |

## Database Schema Support
The service works with your existing `DownloadRequests` table structure and properly sets:
- Parent series linking via `MediaId` and `TvdbId`
- Episode-specific data (`EpisodeId`, `SeasonNumber`, `EpisodeDate`)
- Progress tracking (`DownloadPercentage`, `MinutesLeft`, `Status`)
- Completion timestamps (`CompletedAt` when status = "completed")

## API Endpoint Configuration
The following configuration keys are used:

```
sonarr_download_queue_endpoint = http://192.168.3.120:8989/api/v3/queue?apiKey={ApiKey}&pageSize=1000
sonarr_episode_data_endpoint = http://192.168.3.120:8989/api/v3/episode?seriesId={seriesId}&apiKey={ApiKey}
sonarr_api_key = [your_sonarr_api_key]
```

## Testing
You can manually test the service using the existing test endpoint:
- **Endpoint:** `POST /api/test/process-sonarr-queue`
- **Authorization:** Admin role required
- **Function:** Manually triggers the queue processing logic

## Error Handling
- Comprehensive try-catch blocks around all operations
- Detailed console logging for debugging
- Graceful handling of missing data or API failures
- Service continues running even if individual operations fail

## Performance Optimizations
- Early exit if no active downloads
- Batched API calls by series
- Efficient LINQ queries for database operations
- Minimal API calls using smart caching and grouping

## Next Steps
1. Start your application - the background service will automatically begin monitoring
2. Create some TV show download requests through your existing UI
3. Monitor the console logs to see the service working
4. Check your `DownloadRequests` table to see episodes being added and updated

The service is now production-ready and will automatically keep your download queue synchronized with Sonarr's actual download progress!