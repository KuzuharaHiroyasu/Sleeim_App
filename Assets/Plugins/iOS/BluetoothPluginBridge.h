//
//  BluetoothPluginBridge.h
//  KaiminApp
//
#import <Foundation/Foundation.h>

OBJC_EXTERN void _callBackError(int commandId, int errorType);
OBJC_EXTERN void _callBackConnectionPeripheral(NSString* uuid, NSString* deviceName, NSString* address);
OBJC_EXTERN void _callBackDeviceInfo(NSString* deviceName, NSString* address, NSString* identifierUuid);
OBJC_EXTERN void _callBackBool(int commandId, BOOL isOK);
OBJC_EXTERN void _callBackGetVersion(int g1dAppVerMajor, int g1dAppVerMinor,
                                    int g1dAppVerRevision, int g1dAppVerBuild);
OBJC_EXTERN void _callBackBattery(int batteryLevel);
OBJC_EXTERN void _callBackGetData(int count, BOOL isNext, BOOL isEnd, NSString* tempPath, NSString* fileName);
OBJC_EXTERN void _callBackH1dTransferDataResult(int state);
OBJC_EXTERN void _callBackH1dTransferDataDone(int state, int verMajor,
                                             int verMinor, int verRevision,
                                             int verBuild);
OBJC_EXTERN void _callBackAlarm(int type, BOOL isOn);
OBJC_EXTERN void _callBackWrite(int commandId, BOOL isOK);
OBJC_EXTERN void _callBackBluetoothState(int state);
OBJC_EXTERN void _callBackDeviceStatus(NSString* address,
                                       int dataCount,
                                       int year,
                                       int month,
                                       int day,
                                       int hour,
                                       int minute,
                                       int second,
                                       int weekDay);
OBJC_EXTERN void _callBackNotificationStatus(int status);
