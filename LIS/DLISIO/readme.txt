О проекте

Этот Python проект основан на библиотеке dlisio: https://github.com/equinor/dlisio/tree/master. 
Проект этот поможет тестировать импорт/экспорт .lis и .dlis файлов.
Для запуска нужен python версии 3.12 или новее. Так же понадобиться библиотека dlisio
Запустить можно введя в консоли python: 
main.py 


О библиотеке

Библиотека позволяет читать и писать .lis и .dlis файлы. 
Конкретные версии форматов: 
	DLIS V1(RP66 V1): https://energistics.org/sites/default/files/rp66v1.html
	LIS79: https://energistics.org/sites/default/files/2022-10/lis-79.pdf
Для ознакомления с возможностями рекомендую посмотреть тесты либы на гитхабе: https://github.com/equinor/dlisio/blob/master/python/tests/lis/test_curves.py
В тестах можно увидеть, как читать имена кривых, типы, данные и тд.
Так же настоятельно советую посмотреть документацию к dlisio: https://dlisio.readthedocs.io/en/stable/

Как запускать