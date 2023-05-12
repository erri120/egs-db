function convertToLocalTime() {
    const timeElements = document.getElementsByTagName('time');
    console.log(timeElements);

    for (let timeElement of timeElements) {
        const dateTimeAttribute = timeElement.getAttribute('datetime');
        if (dateTimeAttribute === null) continue;

        const date = Date.parse(dateTimeAttribute);
        const formattedDate = new Intl.DateTimeFormat('default', { dateStyle: 'long', timeStyle: 'long' }).format(date);

        timeElement.textContent = formattedDate;
    }
}

convertToLocalTime();
