# Project Setup and Performance Enhancement Guide

## Initial Configuration Steps

To ensure the application runs smoothly and utilizes performance enhancements, please follow these setup instructions:

1. In the auto-setup, select the appropriate Blog or Company.
2. Enable Mini Tracing:
   - Navigate to `Admin` > `Features`, and toggle on `MiniTracing`.
3. Activate Caching:
   - Go to `Admin` > `Features`, and enable both `Media Caching` and `Distributed Caching`.
4. Turn on Indexing:
   - Enable `Indexing` and `Lucene Indexing` as needed.
5. Use Framework CDN:
   - Access `Admin` > `Settings`, and enable `Use Framework CDN (Content Delivery Network)`.
6. Disable Resource Debug Mode:
   - In `Admin`, find `Resource Debug Mode` and set it to `Disabled` to utilize the minified version of resources.

## Performance Tuning

To enhance the landing page performance, the following modifications have been implemented:

1. **MiniProfiler Integration**:
   - Integrated MiniProfiler for in-depth performance analysis and added custom profiling steps to pinpoint inefficiencies.
2. **Query Optimization**:
   - Analyzed ContentItems queries that were initially taking 20-30 ms to execute.
3. **Index Creation**:
   - Established indexes on frequently queried fields such as `ContentItemId` and `Published` within `ContentItemIndex`, and on `DocumentId` and `UserId` within `UserIndex` for faster data retrieval.
4. **Memory Caching**:
   - Implemented `IMemoryCaching` to cache initial load items, significantly reducing load times.
5. **Cache Invalidity via ISignal**:
   - Employed `ISignal` event mechanism to automatically invalidate cache for specific content items upon their update or deletion by an admin.
6. **GZip Compression**:
   - Applied GZip compression to expedite network traffic, speed up data transfer, and improve user experience.

## Profiling Reports

Included are MiniProfiler snapshots highlighting the potential bottlenecks before and after the aforementioned optimizations:

1. **Before Optimization**:
   - The below report illustrates performance metrics prior to enhancements.
   ![image](https://github.com/chaitanyasrk/OrchardCore/assets/53580000/c33467c6-9af1-456f-aa53-83201bf1bb12)

2. **After Optimization**:
   - The below report demonstrates improved performance metrics following the application of optimizations.
     ![image](https://github.com/chaitanyasrk/OrchardCore/assets/53580000/404ac58c-df62-495b-a4fe-594c7dcda0ec)


Please look at the attached report for a detailed analysis of the performance improvements.
