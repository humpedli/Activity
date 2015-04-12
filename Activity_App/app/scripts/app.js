'use strict';

angular.module('activity', ['ionic', 'ngCordova', 'ngWebSocket', 'LocalStorageModule'])

.run(['$ionicPlatform', '$rootScope', 'localStorageService',
    function($ionicPlatform, $rootScope, localStorageService) {
        $ionicPlatform.ready(function() {
            if(window.cordova && window.cordova.plugins.Keyboard) {
                cordova.plugins.Keyboard.hideKeyboardAccessoryBar(false);
                cordova.plugins.Keyboard.disableScroll(true);
            }
            if(window.StatusBar) {
                StatusBar.styleDefault();
            }
        });
        $rootScope.savedSettings = localStorageService.get('settings') || { ipAddress: '', vibration: true };
        $rootScope.socketStatus = 'disconnected';
    }
])

.config(['$stateProvider', '$urlRouterProvider',
    function($stateProvider, $urlRouterProvider) {
        $stateProvider

        .state('tab', {
            url: "/tab",
            abstract: true,
            templateUrl: "templates/tabs.html"
        })

        .state('tab.activity', {
            url: '/activity',
            views: {
                'tab-activity': {
                    templateUrl: 'templates/tab-activity.html',
                    controller: 'MainController'
                }
            }
        })

        .state('tab.settings', {
            url: '/settings',
            views: {
                'tab-settings': {
                    templateUrl: 'templates/tab-settings.html',
                    controller: 'SettingsController'
                }
            }
        });

        $urlRouterProvider.otherwise('/tab/activity');
    }
])

.controller('MainController', ['$scope', '$rootScope', '$websocket', '$cordovaVibration', 'localStorageService', '$ionicPopup',
    function($scope, $rootScope, $websocket, $cordovaVibration, localStorageService, $ionicPopup) {

        $rootScope.ws;

        $scope.view = {
            ipAddress: '',
            timer: '01:30',
            gameMode: 'NORMAL',
            buttonPressed: false,
            timerStatus: false
        };

        if($rootScope.savedSettings !== undefined) {
            $scope.view.ipAddress = $rootScope.savedSettings.ipAddress;
        }

        $scope.connect = function() {
            $rootScope.socketStatus = 'connecting';

            try {
                if($rootScope.ws !== undefined) {
                    $rootScope.ws.close();
                }
                $rootScope.ws = $websocket('ws://' + $scope.view.ipAddress + ':8000');

                $rootScope.ws.onOpen(onOpen);
                $rootScope.ws.onClose(onClose);
                $rootScope.ws.onMessage(onMessage);
            }
            catch(e) {}

            $rootScope.savedSettings.ipAddress = $scope.view.ipAddress;
            localStorageService.set('settings', $rootScope.savedSettings);
        }

        $scope.action = function(type) {
            if(type == 'startstop') {
                $scope.view.buttonPressed = false;
            }
            $rootScope.send({action: type});
        }

        $scope.gameMode = function(mode) {
            if(mode != $scope.view.gameMode) {
                if($scope.view.timer != '01:30') {
                    $ionicPopup.confirm({
                        title: 'Játékmód változtatása',
                        template: 'Az időzítő és a gombok resetelődnek. Biztosan játékmódot változtatsz?',
                        buttons: [
                            { 
                                text: 'Nem' 
                            },
                            {
                                text: '<b>Igen</b>',
                                type: 'button-orange',
                                onTap: function(e) {
                                    return true;
                                }
                            }
                        ]
                    }).then(function(res) {
                        if(res) {
                            $rootScope.send({action: 'modeSelect', value: mode});
                        }
                    });
                } else {
                    $rootScope.send({action: 'modeSelect', value: mode});
                }
            } 
        }

        $rootScope.send = function(message) {
            setTimeout(function() {
                if($rootScope.ws !== undefined) {
                    $rootScope.ws.send(JSON.stringify(message));
                }
            }, 0);
        }

        var onOpen = function() {
            setTimeout(function() {
                $rootScope.socketStatus = 'connected';
                $scope.$apply();
            }, 0);      
        }

        var onClose = function() {
            setTimeout(function() {
                $rootScope.socketStatus = 'disconnected';
                $scope.$apply();
            }, 0);
        }

        var onMessage = function(message) {
            setTimeout(function() {
                var data = JSON.parse(message.data);
                if(data.action == 'refreshTime') {
                    $scope.view.timer = moment.duration(data.value, 'second').format('mm:ss');
                }
                if(data.action == 'timerStatus') {
                    $scope.view.timerStatus = data.value;
                }
                if(data.action == 'buttonPress') {
                    $scope.view.buttonPressed = data.value;

                    var vibrate = true;
                    if($rootScope.savedSettings !== undefined) {
                        vibrate = $rootScope.savedSettings.vibration;
                    }

                    if(vibrate) {
                        try {
                            $cordovaVibration.vibrate(300);
                        }
                        catch(e) {}
                    }
                }
                if(data.action == 'modeChanged') {
                    $scope.view.gameMode = data.value;
                }
                if(data.action == 'buttonReset') {
                    $scope.view.buttonPressed = 0;
                }
                if(data.action == 'teamsEdited') {
                    $rootScope.teams = data.value;
                }
                if(data.action == 'init') {
                    $scope.view.timer = moment.duration(data.value.timer, 'second').format('mm:ss');
                    $scope.view.gameMode = data.value.gameMode;
                    $scope.view.timerStatus = data.value.timerStatus;
                    $scope.view.buttonPressed = data.value.buttonStatus;
                    $rootScope.teams = data.value.teams;
                }
                $scope.$apply();
            }, 0);
        }
    }
])

.controller('SettingsController', ['$scope', '$rootScope', 'localStorageService',
    function($scope, $rootScope, localStorageService) {
        $scope.view = {
            vibration: $rootScope.savedSettings.vibration,
        }

        $scope.save = function() {
            $rootScope.savedSettings.vibration = $scope.view.vibration;
            localStorageService.set('settings', $rootScope.savedSettings);

            $rootScope.send({action: 'editTeams', value: $rootScope.teams});
        }
    }
]);