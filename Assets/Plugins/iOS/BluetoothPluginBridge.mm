#import <CoreBluetooth/CoreBluetooth.h>
#import <AVFoundation/AVFoundation.h>
#import <UserNotifications/UserNotifications.h>
#import <sleeim-Swift.h>
#import <UIKit/UIKit.h>
#import "BluetoothPluginBridge.h"

#pragma mark  -  C

extern "C" {

    typedef void (*CallBackErrorDelegate)(int commandId, int errorType);
    typedef void (*CallBackConnectionPeripheralDelegate)(char* uuid, char* deviceName, char* address);
    typedef void (*CallBackDeviceInfoDelegate)(char* deviceName, char* address, char* identifierUuid);
    typedef void (*CallBackBoolDelegate)(int commandId, BOOL isOK);
    typedef void (*CallBackGetVersionDelegate)(int g1dAppVerMajor, int g1dAppVerMinor,
                                               int g1dAppVerRevision, int g1dAppVerBuild);
    typedef void (*CallBackBatteryDelegate)(int batteryLevel);
    typedef void (*CallBackGetDataDelegate)(int count, BOOL isNext, BOOL isEnd, char* tempPath, char* fileName);
    typedef void (*CallBackH1dTransferDataResultDelegate)(int state);
    typedef void (*CallBackH1dTransferDataDoneDelegate)(int state, int verMajor,
                                                        int verMinor, int verRevision,
                                                        int verBuild);
    typedef void (*CallBackAlarmDelegate)(int type, BOOL isOn);
    typedef void (*CallBackWriteDelegate)(int commandId, BOOL isOK);
    typedef void (*CallBackBluetoothStateDelegate)(int state);
    typedef void (*CallBackDeviceStatusDelegate)(char* address,
                                                 int dataCount,
                                                 int year,
                                                 int month,
                                                 int day,
                                                 int hour,
                                                 int minute,
                                                 int second,
                                                 int weekDay);
    typedef void (*CallBackNotificationStatusDelegate)(int status);

    CallBackErrorDelegate callBackErrorDelegate;
    CallBackConnectionPeripheralDelegate callBackConnectionPeripheralDelegate;
    CallBackDeviceInfoDelegate callBackDeviceInfoDelegate;
    CallBackBoolDelegate callBackBoolDelegate;
    CallBackGetVersionDelegate callBackGetVersionDelegate;
    CallBackBatteryDelegate callBackBatteryDelegate;
    CallBackGetDataDelegate callBackGetDataDelegate;
    CallBackH1dTransferDataResultDelegate callBackH1dTransferDataResultDelegate;
    CallBackH1dTransferDataDoneDelegate callBackH1dTransferDataDoneDelegate;
    CallBackAlarmDelegate callBackAlarmDelegate;
    CallBackWriteDelegate callBackWriteDelegate;
    CallBackBluetoothStateDelegate callBackBluetoothStateDelegate;
    CallBackDeviceStatusDelegate callBackDeviceStatusDelegate;
    CallBackNotificationStatusDelegate callBackNotificationStatusDelegate;

    void _initialize (CallBackErrorDelegate callBackError,
                      CallBackConnectionPeripheralDelegate callBackConnectionPeripheral,
                      CallBackDeviceInfoDelegate callBackDeviceInfo,
                      CallBackBoolDelegate callBackBool,
                      CallBackGetVersionDelegate callBackGetVersion,
                      CallBackBatteryDelegate callBackBattery,
                      CallBackGetDataDelegate callBackGetData,
                      CallBackH1dTransferDataResultDelegate callBackH1dTransferDataResult,
                      CallBackH1dTransferDataDoneDelegate callBackH1dTransferDataDone,
                      CallBackAlarmDelegate callBackAlarm,
                      CallBackWriteDelegate callBackWrite,
                      CallBackBluetoothStateDelegate callBackBluetoothState,
                      CallBackDeviceStatusDelegate callBackDeviceStatus,
  CallBackNotificationStatusDelegate callBackNotificationStatus) {

        callBackErrorDelegate = callBackError;
        callBackConnectionPeripheralDelegate = callBackConnectionPeripheral;
        callBackDeviceInfoDelegate = callBackDeviceInfo;
        callBackBoolDelegate = callBackBool;
        callBackGetVersionDelegate = callBackGetVersion;
        callBackBatteryDelegate = callBackBattery;
        callBackGetDataDelegate = callBackGetData;
        callBackH1dTransferDataResultDelegate = callBackH1dTransferDataResult;
        callBackH1dTransferDataDoneDelegate = callBackH1dTransferDataDone;
        callBackAlarmDelegate = callBackAlarm;
        callBackWriteDelegate = callBackWrite;
        callBackBluetoothStateDelegate = callBackBluetoothState;
        callBackDeviceStatusDelegate = callBackDeviceStatus;
        callBackNotificationStatusDelegate = callBackNotificationStatus;
        [[BluetoothPlugin shared] initialize];
    }

    void _deInitialize () {
        [[BluetoothPlugin shared] deInitialize];
    }

    void _scanStart () {
        [[BluetoothPlugin shared] scanStart];
    }

    void _scanStop () {
        [[BluetoothPlugin shared] scanStop];
    }

    void _connectionPeripheral (char* identifierUuid) {
    	NSString* identifierUuid2 = [NSString stringWithCString: identifierUuid encoding:NSUTF8StringEncoding];
        [[BluetoothPlugin shared] connectionPeripheral: identifierUuid2];
    }

    void _reConnectionPeripheral (char* uuid) {
        NSString* identifier = [NSString stringWithCString: uuid encoding:NSUTF8StringEncoding];
        [[BluetoothPlugin shared] reConnectionPeripheral: identifier];
    }

    void _sendCommand(int commandId) {
        [[BluetoothPlugin shared] sendCommand: commandId];
    }

    void _sendBleCommand(int commandId, unsigned char *data, int length) {
        NSData *value = [[NSData alloc] init];
        if (data != nil) {
            value = [[NSData alloc] initWithBytes:data length:length];
        }
        [[BluetoothPlugin shared] sendBleCommand: commandId
                                           value: value];
    }

    void _changeServiceUUIDToNormal() {
        [[BluetoothPlugin shared] changeServiceUUIDToNormal];
    }

    void _changeServiceUUIDToFirmwareUpdate() {
        [[BluetoothPlugin shared] changeServiceUUIDToFirmwareUpdate];
    }

    void _changeCharacteristicUUIDToFirmwareUpdateControl() {
        [[BluetoothPlugin shared] changeCharacteristicUUIDToFirmwareUpdateControl];
    }

    void _changeCharacteristicUUIDToFirmwareUpdateData() {
        [[BluetoothPlugin shared] changeCharacteristicUUIDToFirmwareUpdateData];
    }

    void _sendGetEnd(BOOL isOK) {
        [[BluetoothPlugin shared] sendGetEnd: isOK];
    }

    void _sendDateSetting(char* date) {
        NSString* dateStr = [NSString stringWithCString: date encoding:NSUTF8StringEncoding];
        [[BluetoothPlugin shared] sendDateSetting: dateStr];
    }

    void _sendH1DDate(unsigned char *data, int length) {
        NSData *value = [[NSData alloc] init];
        if (data != nil) {
            value = [[NSData alloc] initWithBytes:data length:length];
        }
        [[BluetoothPlugin shared] sendH1DDate: value];
    }

    void _sendH1DCheckSum(unsigned char *data, int length) {
        NSData *value = [[NSData alloc] init];
        if (data != nil) {
            value = [[NSData alloc] initWithBytes:data length:length];
        }
        [[BluetoothPlugin shared] sendH1DCheckSum: value];
    }

    void _setCsvHeaderInfo(char* deviceId, char* nickname, char* sex,
                           char* birthday, char* tall, char* weight,
                           char* sleepStartTime, char* sleepEndTime, char* g1dVersion) {
        NSString* deviceIdStr = [NSString stringWithCString: deviceId encoding:NSUTF8StringEncoding];
        NSString* nicknameStr = [NSString stringWithCString: nickname encoding:NSUTF8StringEncoding];
        NSString* sexStr = [NSString stringWithCString: sex encoding:NSUTF8StringEncoding];
        NSString* birthdayStr = [NSString stringWithCString: birthday encoding:NSUTF8StringEncoding];
        NSString* tallStr = [NSString stringWithCString: tall encoding:NSUTF8StringEncoding];
        NSString* weightStr = [NSString stringWithCString: weight encoding:NSUTF8StringEncoding];
        NSString* sleepStartTimeStr = [NSString stringWithCString: sleepStartTime encoding:NSUTF8StringEncoding];
        NSString* sleepEndTimeStr = [NSString stringWithCString: sleepEndTime encoding:NSUTF8StringEncoding];
        NSString* g1dVersionStr = [NSString stringWithCString: g1dVersion encoding:NSUTF8StringEncoding];

        [[BluetoothPlugin shared] setCsvHeaderInfo: deviceIdStr
                                          nickname: nicknameStr
                                               sex: sexStr
                                          birthday: birthdayStr
                                              tall: tallStr
                                            weight: weightStr
                                    sleepStartTime: sleepStartTimeStr
                                      sleepEndTime: sleepEndTimeStr
                                        g1dVersion: g1dVersionStr];
    }

    void _sendAlarmSetting(int alarm, int snoreAlarm, int snoreSensitivity, int apneaAlarm,
                           int alarmDelay, int bodyMoveStop, int alramTime) {
        [[BluetoothPlugin shared] sendAlarmSetting:alarm
                                        snoreAlarm:snoreAlarm
                                  snoreSensitivity:snoreSensitivity
                                        apneaAlarm:apneaAlarm
                                        alarmDelay:alarmDelay
                                      bodyMoveStop:bodyMoveStop
                                         alramTime:alramTime];
    }

    BOOL _checkBluetoothSupported() {
        return [[BluetoothPlugin shared] checkBluetoothSupported];
    }

    BOOL _checkBluetoothPoweredOn() {
        return [[BluetoothPlugin shared] checkBluetoothPoweredOn];
    }

    /**
     * 設定画面を開く（BLE用）
     */
    void _openBLESetting() {
        [[BluetoothPlugin shared] openBLESetting];
    }

    /**
     * 設定画面を開く（ローカル通知用）
     */
    void _openLocalNotificationSetting() {
        [[BluetoothPlugin shared] openLocalNotificationSetting];
    }

    /**
     * ローカル通知の設定を返す
     */
    void _checkLocalNotificationSetting() {
        [[BluetoothPlugin shared] checkLocalNotificationSetting];
    }

    /**
     * 接続中断or切断処理
     */
    void _disConnectPeripheral() {
        [[BluetoothPlugin shared] disConnectPeripheral];
    }

    /**
     * アラーム停止
     */
    void _stopAlarm() {
        [[BluetoothPlugin shared] stopAlarm];
    }

    void _callBackError(int commandId, int errorType) {
//        NSLog(@"_callBackError commandId: %@ errorType: %@",
//              @(commandId).stringValue, @(errorType).stringValue);
        callBackErrorDelegate(commandId, errorType);
    }

    void _callBackConnectionPeripheral(NSString* uuid, NSString* deviceName, NSString* address) {
//        NSLog(@"_callBackConnectionPeripheral uuid: %@ deviceName: %@",
//              uuid, deviceName);
        callBackConnectionPeripheralDelegate((char*)[uuid UTF8String], (char*)[deviceName UTF8String], (char*)[address UTF8String]);
    }

    void _callBackDeviceInfo(NSString* deviceName, NSString* address, NSString* identifierUuid) {
//        NSLog(@"_callBackDeviceInfo deviceName: %@ identifierUuid: %@",
//              deviceName, identifierUuid);
        callBackDeviceInfoDelegate((char*)[deviceName UTF8String], (char*)[address UTF8String], (char*)[identifierUuid UTF8String]);
    }

    void _callBackBool(int commandId, BOOL isOK) {
//        NSLog(@"_callBackBool commandId: %@ isOK: %@",
//              @(commandId).stringValue, @(isOK).stringValue);
        callBackBoolDelegate(commandId, isOK);
    }

    void _callBackGetVersion(int g1dAppVerMajor, int g1dAppVerMinor,
                             int g1dAppVerRevision, int g1dAppVerBuild) {
//        NSLog(@"_callBackGetVersion h1dAppVerMajor: %@ h1dAppVerMinor: %@ h1dAppVerRevision: %@ h1dAppVerBuild: %@ h1dBootVerMajor: %@ h1dBootVerMinor: %@ h1dBootVerRevision: %@ h1dBootVerBuild: %@ g1dAppVerMajor: %@ g1dAppVerMinor: %@ g1dAppVerRevision: %@ g1dAppVerBuild: %@",
//              @(h1dAppVerMajor).stringValue, @(h1dAppVerMinor).stringValue,
//              @(h1dAppVerRevision).stringValue, @(h1dAppVerBuild).stringValue,
//              @(h1dBootVerMajor).stringValue, @(h1dBootVerMinor).stringValue,
//              @(h1dBootVerRevision).stringValue, @(h1dBootVerBuild).stringValue,
//              @(g1dAppVerMajor).stringValue, @(g1dAppVerMinor).stringValue,
//              @(g1dAppVerRevision).stringValue, @(g1dAppVerBuild).stringValue);
        callBackGetVersionDelegate(g1dAppVerMajor, g1dAppVerMinor, g1dAppVerRevision, g1dAppVerBuild);
    }

    void _callBackBattery(int batteryLevel) {
//        NSLog(@"_callBackBattery batteryLevel: %@",
//              @(batteryLevel).stringValue);
        callBackBatteryDelegate(batteryLevel);
    }

    void _callBackGetData(int count, BOOL isNext, BOOL isEnd, NSString* tempPath, NSString* fileName) {
        char* temp = NULL;
        if (tempPath) {
            temp = (char*)[tempPath UTF8String];
        } else {
            temp = (char*)[@"" UTF8String];
        }
        char* file = NULL;
        if (fileName) {
            file = (char*)[fileName UTF8String];
        } else {
            file = (char*)[@"" UTF8String];
        }
//        NSLog(@"_callBackGetData count: %@ isNext: %@ isEnd: %@ tempPath: %@ fileName: %@",
//              @(count).stringValue, @(isNext).stringValue,
//              @(isEnd).stringValue, tempPath,
//              fileName);
        callBackGetDataDelegate(count, isNext, isEnd, temp, file);
    }

    void _callBackH1dTransferDataResult(int state) {
//        NSLog(@"_callBackH1dTransferDataResult state: %@",
//              @(state).stringValue);
        callBackH1dTransferDataResultDelegate(state);
    }

    void _callBackH1dTransferDataDone(int state, int verMajor,
                                      int verMinor, int verRevision,
                                      int verBuild) {
//        NSLog(@"_callBackH1dTransferDataDone state: %@ verMajor: %@ verMinor: %@ verRevision: %@ verBuild: %@",
//              @(state).stringValue, @(verMajor).stringValue,
//              @(verMinor).stringValue, @(verRevision).stringValue,
//              @(verBuild).stringValue);
        callBackH1dTransferDataDoneDelegate(state, verMajor, verMinor, verRevision, verBuild);
    }

    void _callBackAlarm(int type, BOOL isOn) {
//        NSLog(@"_callBackAlarm type: %@ isOn: %@",
//              @(type).stringValue, @(isOn).stringValue);
        callBackAlarmDelegate(type, isOn);
    }

    void _callBackWrite(int commandId, BOOL isOK) {
//        NSLog(@"_callBackWrite commandId: %@ isOK: %@",
//              @(commandId).stringValue, @(isOK).stringValue);
        callBackWriteDelegate(commandId, isOK);
    }

    void _callBackBluetoothState(int state) {
//        NSLog(@"_callBackBluetoothState state: %@",
//              @(state).stringValue);
        callBackBluetoothStateDelegate(state);
    }

    void _callBackDeviceStatus(NSString* address,
                               int dataCount,
                               int year,
                               int month,
                               int day,
                               int hour,
                               int minute,
                               int second,
                               int weekDay) {

//        NSLog(@"_callBackDeviceStatus address: %@ dataCount: %@ year: %@ month: %@ day: %@ hour: %@ minute: %@ second: %@ weekDay: %@",
//              address, @(dataCount).stringValue,
//              @(year).stringValue, @(month).stringValue,
//              @(day).stringValue, @(hour).stringValue,
//              @(minute).stringValue, @(second).stringValue,
//              @(weekDay).stringValue);
        callBackDeviceStatusDelegate((char*)[address UTF8String],
                                     dataCount, year, month, day, hour, minute, second, weekDay);
    }

    void _callBackNotificationStatus(int status) {
//        NSLog(@"_callBackNotificationStatus status: %@",
//              @(status).stringValue);
        callBackNotificationStatusDelegate(status);
    }
}
