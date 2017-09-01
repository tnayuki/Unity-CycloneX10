//
//  Plugin.m
//  CycloneX10Plugin
//
//  Created by Toru Nayuki on 2017/08/21.
//
//

#import <IOKit/hid/IOHIDManager.h>

static BOOL _isInitialized = NO;

static NSMutableArray *_devices = nil;
static IOHIDManagerRef _manager = NULL;

static void Handle_IOHIDDeviceMatchingCallback(void *context, IOReturn result, void *sender, IOHIDDeviceRef device) {
    if (result == kIOReturnSuccess) {
        [_devices addObject:CFBridgingRelease(device)];
    }
}

void InitializePlugin()
{
    if (_isInitialized) return;
    
    _devices = [NSMutableArray new];
    
    _manager = IOHIDManagerCreate(kCFAllocatorDefault, kIOHIDManagerOptionNone);
    
    IOHIDManagerSetDeviceMatching(_manager,
                                  (CFDictionaryRef)@{
                                                     @kIOHIDVendorIDKey: @(0x0483),
                                                      @kIOHIDProductIDKey: @(0x5750)
                                                      });
    
    IOHIDManagerScheduleWithRunLoop(_manager, CFRunLoopGetCurrent(), kCFRunLoopDefaultMode);
    IOReturn rv = IOHIDManagerOpen(_manager, kIOHIDOptionsTypeSeizeDevice);

    if (rv == kIOReturnSuccess) {
        IOHIDManagerRegisterDeviceMatchingCallback(_manager, &Handle_IOHIDDeviceMatchingCallback, NULL);
    }
    
    _isInitialized = YES;
}

void DeinitializePlugin()
{
    CFRelease(_manager);
    
    _devices = nil;
    
    _isInitialized = NO;
}

int CountInternal()
{
    return (int)[_devices count];
}

void VibrateInternal(int index, int pattern, int level)
{
    uint8_t report[15] = { 0x3C, 0x30, 0x31, 0x35, 0x32, 0x30, (0x30 + pattern), (0x30 + level), 0x30, 0x30, 0x01, 0x02, 0x03, 0x68, 0x3E };
    
    if (index < 0) {
        for (NSObject *device in _devices) {
            IOHIDDeviceSetReport((IOHIDDeviceRef)device, kIOHIDReportTypeOutput, 0, report, 15);
        }
    } else if (index < [_devices count]) {
        IOHIDDeviceSetReport((IOHIDDeviceRef)_devices[index], kIOHIDReportTypeOutput, 0, report, 15);
    }
}
