---
title: Interface IHTTP.Body
sidebar_label: IHTTP.Body
---
# Interface IHTTP.Body


###### **Assembly**: Netly.dll
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.Body.cs#L7)
```csharp title="Declaration"
public interface IHTTP.Body
```
## Properties
### Enctype
Enctype type
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.Body.cs#L12)
```csharp title="Declaration"
HTTP.Enctype Enctype { get; }
```
### Text
Text buffer
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.Body.cs#L17)
```csharp title="Declaration"
string Text { get; }
```
### Binary
Binary buffer
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.Body.cs#L22)
```csharp title="Declaration"
byte[] Binary { get; }
```
### TextQueries
Get value from Enctype content (return string)
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.Body.cs#L27)
```csharp title="Declaration"
Dictionary<string, string> TextQueries { get; }
```
### BinaryQueries
Get value from Enctype content (return bytes)
###### [View Source](https://github.com/alec1o/Netly/blob/dev/src/http/interfaces/IHTTP.Body.cs#L32)
```csharp title="Declaration"
Dictionary<string, byte[]> BinaryQueries { get; }
```
