let form = document.querySelector('.user-items-footer form');

let plus = document.querySelector('.user-items-footer .plus');
let minus = document.querySelector('.user-items-footer .minus');

let input = document.querySelector('.user-items-footer input[type=number]');

plus.addEventListener('click', event => {
    let value = input.valueAsNumber;
    input.stepUp();
    if (input.valueAsNumber !== value)
        invokeChange(input);
})

minus.addEventListener('click', event => {
    let value = input.valueAsNumber;
    input.stepDown();
    if (input.valueAsNumber !== value)
        invokeChange(input);
})

input.addEventListener('change', event => {
    form.submit();
})


function invokeChange(object){
    const ev = new Event('change');
    object.dispatchEvent(ev);
}
